using System.Diagnostics.CodeAnalysis;

namespace ItemFinder.Application;

/// <summary>Use case: parse the data file and build the item directory in one step.</summary>
public sealed class ItemDirectoryLoader(IDataFileParser parser)
{
    public DirectoryLoadResult Load(string dataFilePath)
    {
        var result = parser.ParseFile(dataFilePath);
        return result.Success
            ? DirectoryLoadResult.Ok(new ItemDirectory(result.Forest))
            : DirectoryLoadResult.Failed(result.Errors);
    }
}

/// <summary>Outcome of loading a data file: either a directory or one or more errors, never both.</summary>
public sealed class DirectoryLoadResult
{
    private DirectoryLoadResult(ItemDirectory? directory, IReadOnlyList<ParseError> errors)
    {
        Directory = directory;
        Errors = errors;
    }

    public ItemDirectory? Directory { get; }

    public IReadOnlyList<ParseError> Errors { get; }

    [MemberNotNullWhen(true, nameof(Directory))]
    public bool Success => Directory is not null;

    public static DirectoryLoadResult Ok(ItemDirectory directory) => new(directory, []);

    public static DirectoryLoadResult Failed(IReadOnlyList<ParseError> errors) => new(null, errors);
}
