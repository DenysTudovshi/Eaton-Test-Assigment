using System.Diagnostics.CodeAnalysis;

using ItemFinder.Domain.Entities;
using ItemFinder.Domain.ValueObjects;

namespace ItemFinder.Application.Results;

/// <summary>Outcome of parsing a data file: either a forest or one or more errors, never both.</summary>
public sealed class ParseResult
{
    private ParseResult(DirectionForest? forest, IReadOnlyList<ParseError> errors)
    {
        Forest = forest;
        Errors = errors;
    }

    public DirectionForest? Forest { get; }

    public IReadOnlyList<ParseError> Errors { get; }

    [MemberNotNullWhen(true, nameof(Forest))]
    public bool Success => Forest is not null;

    public static ParseResult Ok(DirectionForest forest) => new(forest, []);

    public static ParseResult Failed(ParseError error) => Failed([error]);

    public static ParseResult Failed(IEnumerable<ParseError> errors)
    {
        var list = errors.ToList();
        return list.Count == 0
            ? throw new ArgumentException("A failed parse must carry at least one error.", nameof(errors))
            : new ParseResult(null, list);
    }
}