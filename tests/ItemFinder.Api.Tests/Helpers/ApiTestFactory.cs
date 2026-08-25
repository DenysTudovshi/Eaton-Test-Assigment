using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ItemFinder.Api.Tests.Helpers;

/// <summary>Hosts the API over an isolated temp data-file store, seeded with the canonical file unless disabled.</summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "ItemFinderApiTests", Guid.NewGuid().ToString("N"));

    private readonly bool _seed;

    public ApiTestFactory()
        : this(seed: true)
    {
    }

    private ApiTestFactory(bool seed)
    {
        _seed = seed;
    }

    /// <summary>A factory whose store starts empty instead of seeded.</summary>
    public static ApiTestFactory Unseeded() => new(seed: false);

    public static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["DataFile:StoragePath"] = Path.Combine(_root, "Data.txt"),
            ["DataFile:SeedPath"] = _seed ? FixturePath("Data.txt") : string.Empty,
        };

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
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
    }
}