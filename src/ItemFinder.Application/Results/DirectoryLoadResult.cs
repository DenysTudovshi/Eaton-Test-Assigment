using System.Diagnostics.CodeAnalysis;

using ItemFinder.Application.Services;

namespace ItemFinder.Application.Results;

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