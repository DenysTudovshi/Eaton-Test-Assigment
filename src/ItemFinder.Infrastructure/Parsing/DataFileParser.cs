using ItemFinder.Application.Enums;
using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Results;
using ItemFinder.Application.Services;
using ItemFinder.Domain.Entities;
using ItemFinder.Domain.ValueObjects;

namespace ItemFinder.Infrastructure.Parsing;

/// <summary>
/// Parses the hierarchical data file format: a root direction per "+ " line, and
/// branch lines made of 3-character prefix groups ("|  " or "   "), a tree glyph
/// ("├──" or "└──"), and either a nested direction ("+ ") or an item (" Item: ").
/// </summary>
public sealed class DataFileParser : IDataFileParser
{
    private const char ByteOrderMark = '﻿';
    private const string RootMarker = "+ ";
    private const string DirectionMarker = "+ ";
    private const string ItemMarker = " Item: ";
    private const int GroupWidth = 3;
    private static readonly string[] PrefixGroups = ["|  ", "   "];
    private static readonly string[] Glyphs = ["├──", "└──"];

    public ParseResult ParseFile(string path)
    {
        // Decided up front: Windows fails to open a directory, but Linux opens it and
        // only the read fails, with an exception that would map to a different error.
        if (Directory.Exists(path))
        {
            return ParseResult.Failed(new ParseError(ParseErrorKind.FileNotFound, $"The data file '{path}' could not be found."));
        }

        string text;
        try
        {
            text = File.ReadAllText(path);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            return ParseResult.Failed(new ParseError(ParseErrorKind.FileNotFound, $"The data file '{path}' could not be found."));
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return ParseResult.Failed(new ParseError(ParseErrorKind.FileUnreadable, $"The data file '{path}' could not be read."));
        }

        return ParseText(text);
    }

    public ParseResult ParseText(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        var lines = SplitLines(text);

        var roots = new List<DirectionNode>();
        var openDirections = new List<DirectionNode>(); // index == depth of the open direction
        var seenItems = new HashSet<string>(StringComparer.Ordinal);
        var lastLineWasItem = false;

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];
            var lineNumber = index + 1;

            if (line.Length == 0)
            {
                return ParseResult.Failed(new ParseError(ParseErrorKind.BlankLine,
                    $"Line {lineNumber} is blank; the data file must not contain blank lines.", lineNumber));
            }

            if (line.StartsWith(RootMarker, StringComparison.Ordinal))
            {
                var root = new DirectionNode(line[RootMarker.Length..].Trim());
                roots.Add(root);
                openDirections.Clear();
                openDirections.Add(root);
                lastLineWasItem = false;
                continue;
            }

            if (roots.Count == 0)
            {
                return ParseResult.Failed(new ParseError(ParseErrorKind.FirstLineNotRoot,
                    "Line 1 must be a root direction starting with '+ '.", lineNumber));
            }

            var position = 0;
            while (MatchesAny(line, position, PrefixGroups))
            {
                position += GroupWidth;
            }

            if (!MatchesAny(line, position, Glyphs))
            {
                return Malformed(lineNumber);
            }

            var depth = position / GroupWidth + 1;
            var tail = line[(position + Glyphs[0].Length)..];

            if (depth > openDirections.Count)
            {
                return lastLineWasItem && depth == openDirections.Count + 1
                    ? ParseResult.Failed(new ParseError(ParseErrorKind.NestedUnderItem,
                        $"Line {lineNumber} is nested under an item; items cannot contain anything beneath them.", lineNumber))
                    : ParseResult.Failed(new ParseError(ParseErrorKind.SkippedLevel,
                        $"Line {lineNumber} skips a level of the hierarchy.", lineNumber));
            }

            var parent = openDirections[depth - 1];

            if (tail.StartsWith(DirectionMarker, StringComparison.Ordinal))
            {
                var direction = new DirectionNode(tail[DirectionMarker.Length..].Trim());
                parent.AddChild(direction);
                openDirections.RemoveRange(depth, openDirections.Count - depth);
                openDirections.Add(direction);
                lastLineWasItem = false;
            }
            else if (tail.StartsWith(ItemMarker, StringComparison.Ordinal))
            {
                var item = new ItemNode(tail[ItemMarker.Length..]);
                if (!seenItems.Add(item.Name))
                {
                    return ParseResult.Failed(new ParseError(ParseErrorKind.DuplicateItem,
                        $"Line {lineNumber} repeats the item '{item.Name}'; item names must be unique.", lineNumber));
                }

                parent.AddChild(item);
                openDirections.RemoveRange(depth, openDirections.Count - depth);
                lastLineWasItem = true;
            }
            else
            {
                return Malformed(lineNumber);
            }
        }

        if (roots.Count == 0)
        {
            return ParseResult.Failed(new ParseError(ParseErrorKind.EmptyFile, "The data file is empty."));
        }

        return ParseResult.Ok(new DirectionForest(roots));
    }

    private static ParseResult Malformed(int lineNumber) =>
        ParseResult.Failed(new ParseError(ParseErrorKind.MalformedLine,
            $"Line {lineNumber} is not a valid direction or item line.", lineNumber));

    private static bool MatchesAny(string line, int position, string[] candidates)
    {
        foreach (var candidate in candidates)
        {
            if (position + candidate.Length <= line.Length
                && string.CompareOrdinal(line, position, candidate, 0, candidate.Length) == 0)
            {
                return true;
            }
        }

        return false;
    }


    private static List<string> SplitLines(string text)
    {
        if (text.StartsWith(ByteOrderMark))
        {
            text = text[1..];
        }

        var lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').ToList();

        // A single trailing newline produces one empty final entry; tolerate it.
        if (lines.Count > 0 && lines[^1].Length == 0)
        {
            lines.RemoveAt(lines.Count - 1);
        }

        return lines;
    }
}