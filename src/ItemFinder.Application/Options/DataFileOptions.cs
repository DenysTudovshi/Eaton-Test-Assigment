namespace ItemFinder.Application.Options;

/// <summary>Settings for the managed data-file store, bound from configuration.</summary>
public sealed class DataFileOptions
{
    public const string SectionName = "DataFile";

    /// <summary>Full path of the writable managed copy of the data file.</summary>
    public string StoragePath { get; set; } = string.Empty;

    /// <summary>Bundled default data file used to seed an empty store; null starts empty.</summary>
    public string? SeedPath { get; set; }

    /// <summary>Upper bound accepted for uploaded data files.</summary>
    public long MaxSizeBytes { get; set; } = 1024 * 1024;
}