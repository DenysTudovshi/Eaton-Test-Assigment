using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Options;
using ItemFinder.Application.Results;
using ItemFinder.Application.Services;

namespace ItemFinder.Infrastructure.Storage;

/// <summary>
/// File-system store for the managed data file. Replaces are parse-gated and written
/// atomically (temp file + move); the parsed directory is cached and swapped under a
/// lock, so readers always see a directory matching the stored content.
/// </summary>
public sealed class ManagedDataFileStore : IManagedDataFileStore
{
    private readonly object _sync = new();
    private readonly IDataFileParser _parser;
    private readonly string _storagePath;
    private ItemDirectory? _directory;

    public ManagedDataFileStore(DataFileOptions options, IDataFileParser parser)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(parser);
        ArgumentException.ThrowIfNullOrWhiteSpace(options.StoragePath);

        _parser = parser;
        _storagePath = options.StoragePath;

        var storageDirectory = Path.GetDirectoryName(_storagePath);
        if (!string.IsNullOrEmpty(storageDirectory))
        {
            Directory.CreateDirectory(storageDirectory);
        }

        if (File.Exists(_storagePath))
        {
            LoadExistingFile();
        }
        else if (options.SeedPath is { Length: > 0 } seedPath && File.Exists(seedPath))
        {
            Replace(File.ReadAllText(seedPath));
        }
    }

    public ItemDirectory? CurrentDirectory
    {
        get
        {
            lock (_sync)
            {
                return _directory;
            }
        }
    }

    public string? ReadContent()
    {
        lock (_sync)
        {
            return File.Exists(_storagePath) ? File.ReadAllText(_storagePath) : null;
        }
    }

    public DataFileReplaceResult Replace(string content)
    {
        ArgumentNullException.ThrowIfNull(content);

        lock (_sync)
        {
            var parsed = _parser.ParseText(content);
            if (!parsed.Success)
            {
                return DataFileReplaceResult.Invalid(parsed.Errors);
            }

            var directory = new ItemDirectory(parsed.Forest);
            var createdNew = !File.Exists(_storagePath);

            var tempPath = _storagePath + ".tmp";
            File.WriteAllText(tempPath, content);
            File.Move(tempPath, _storagePath, overwrite: true);

            _directory = directory;
            return DataFileReplaceResult.Ok(createdNew, directory.Items.Count);
        }
    }

    public void Delete()
    {
        lock (_sync)
        {
            if (File.Exists(_storagePath))
            {
                File.Delete(_storagePath);
            }

            _directory = null;
        }
    }

    private void LoadExistingFile()
    {
        var parsed = _parser.ParseText(File.ReadAllText(_storagePath));

        // An unparseable pre-existing file is kept on disk but exposes no items;
        // the next valid replace restores consistency.
        _directory = parsed.Success ? new ItemDirectory(parsed.Forest) : null;
    }
}