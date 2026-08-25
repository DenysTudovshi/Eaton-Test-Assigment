using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Options;
using ItemFinder.Application.Results;
using ItemFinder.Infrastructure.Parsing;
using ItemFinder.Infrastructure.Storage;

namespace ItemFinder.Infrastructure.Tests;

public sealed class ManagedDataFileStoreTests : IDisposable
{
    private const string OneItemContent = "+ Room\n└── Item: Lamp";

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "ItemFinderStoreTests", Guid.NewGuid().ToString("N"));

    private string StoragePath => Path.Combine(_root, "Data.txt");

    [Fact]
    public void Ctor_WithSeed_SeedsStoreAndDirectory()
    {
        var store = CreateStore(seedPath: FixturePath("Data.txt"));

        Assert.Equal(File.ReadAllText(FixturePath("Data.txt")), store.ReadContent());
        Assert.NotNull(store.CurrentDirectory);
        Assert.Equal(5, store.CurrentDirectory.Items.Count);
    }

    [Fact]
    public void Ctor_WithoutSeed_StartsEmpty()
    {
        var store = CreateStore(seedPath: null);

        Assert.Null(store.ReadContent());
        Assert.Null(store.CurrentDirectory);
    }

    [Fact]
    public void Ctor_WithExistingManagedFile_LoadsItWithoutReseeding()
    {
        Directory.CreateDirectory(_root);
        File.WriteAllText(StoragePath, OneItemContent);

        var store = CreateStore(seedPath: FixturePath("Data.txt"));

        Assert.Equal(OneItemContent, store.ReadContent());
        Assert.NotNull(store.CurrentDirectory);
        var item = Assert.Single(store.CurrentDirectory.Items);
        Assert.Equal("Lamp", item.Name);
    }

    [Fact]
    public void Replace_ValidContent_UpdatesContentAndDirectory()
    {
        var store = CreateStore(seedPath: FixturePath("Data.txt"));
        var mediumContent = File.ReadAllText(FixturePath("Data-medium.txt"));

        var result = store.Replace(mediumContent);

        Assert.True(result.Success);
        Assert.False(result.CreatedNew);
        Assert.Equal(9, result.ItemCount);
        Assert.Equal(mediumContent, store.ReadContent());
        Assert.NotNull(store.CurrentDirectory);
        Assert.Equal(9, store.CurrentDirectory.Items.Count);
    }

    [Fact]
    public void Replace_OnEmptyStore_ReportsCreatedNew()
    {
        var store = CreateStore(seedPath: null);

        var result = store.Replace(OneItemContent);

        Assert.True(result.Success);
        Assert.True(result.CreatedNew);
        Assert.Equal(1, result.ItemCount);
    }

    [Fact]
    public void Replace_InvalidContent_ReportsErrorsAndChangesNothing()
    {
        var store = CreateStore(seedPath: FixturePath("Data.txt"));
        var originalContent = store.ReadContent();

        var result = store.Replace("this is not a data file");

        Assert.False(result.Success);
        Assert.NotEmpty(result.Errors);
        Assert.Equal(originalContent, store.ReadContent());
        Assert.NotNull(store.CurrentDirectory);
        Assert.Equal(5, store.CurrentDirectory.Items.Count);
    }

    [Fact]
    public void Delete_RemovesFileAndDirectory_AndIsIdempotent()
    {
        var store = CreateStore(seedPath: FixturePath("Data.txt"));

        store.Delete();

        Assert.Null(store.ReadContent());
        Assert.Null(store.CurrentDirectory);
        store.Delete();
        Assert.Null(store.ReadContent());
    }

    [Fact]
    public void Replace_ParsesOncePerReplace_NotPerRead()
    {
        var parser = new CountingParser();
        var store = CreateStore(seedPath: null, parser);

        store.Replace(OneItemContent);
        _ = store.CurrentDirectory;
        _ = store.CurrentDirectory;
        _ = store.ReadContent();

        Assert.Equal(1, parser.ParseTextCalls);
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(_root, recursive: true);
        }
        catch (DirectoryNotFoundException)
        {
        }
        catch (IOException)
        {
        }

        GC.SuppressFinalize(this);
    }

    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    private ManagedDataFileStore CreateStore(string? seedPath, IDataFileParser? parser = null) =>
        new(
            new DataFileOptions { StoragePath = StoragePath, SeedPath = seedPath },
            parser ?? new DataFileParser());

    private sealed class CountingParser : IDataFileParser
    {
        private readonly DataFileParser _inner = new();

        public int ParseTextCalls { get; private set; }

        public ParseResult ParseFile(string path) => _inner.ParseFile(path);

        public ParseResult ParseText(string text)
        {
            ParseTextCalls++;
            return _inner.ParseText(text);
        }
    }
}