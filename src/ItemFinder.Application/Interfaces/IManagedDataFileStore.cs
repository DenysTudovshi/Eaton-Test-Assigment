using ItemFinder.Application.Results;
using ItemFinder.Application.Services;

namespace ItemFinder.Application.Interfaces;

/// <summary>
/// The writable data file behind the API together with the directory parsed from it.
/// Implementations keep file content and directory consistent under concurrent use;
/// invalid content is never stored.
/// </summary>
public interface IManagedDataFileStore
{
    /// <summary>Directory parsed from the stored file, or null when no file is stored.</summary>
    ItemDirectory? CurrentDirectory { get; }

    /// <summary>The stored file's content, or null when no file is stored.</summary>
    string? ReadContent();

    /// <summary>Validates <paramref name="content"/> and atomically replaces the stored file; invalid content changes nothing.</summary>
    DataFileReplaceResult Replace(string content);

    /// <summary>Removes the stored file; succeeds even when nothing is stored.</summary>
    void Delete();
}