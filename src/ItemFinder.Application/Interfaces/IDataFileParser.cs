using ItemFinder.Application.Results;

namespace ItemFinder.Application.Interfaces;

/// <summary>Parses the hierarchical data file format into a direction forest.</summary>
public interface IDataFileParser
{
    /// <summary>Parses the file at <paramref name="path"/>; IO problems become errors in the result.</summary>
    ParseResult ParseFile(string path);

    /// <summary>Parses raw data file content.</summary>
    ParseResult ParseText(string text);
}