using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ItemFinder.Api.Tests.Helpers;

/// <summary>
/// Hosts the API over an isolated temp data-file store and identity database.
/// Seeded with the canonical data file and a configured admin unless disabled.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin!Passw0rd";

    private readonly string _root =
        Path.Combine(Path.GetTempPath(), "ItemFinderApiTests", Guid.NewGuid().ToString("N"));

    private readonly bool _seed;
    private readonly bool _withAdmin;

    public ApiTestFactory()
        : this(seed: true, withAdmin: true)
    {
    }

    private ApiTestFactory(bool seed, bool withAdmin)
    {
        _seed = seed;
        _withAdmin = withAdmin;
    }

    /// <summary>A factory whose data-file store starts empty instead of seeded.</summary>
    public static ApiTestFactory Unseeded() => new(seed: false, withAdmin: true);

    /// <summary>A factory with no admin credentials configured, so no admin is seeded.</summary>
    public static ApiTestFactory WithoutAdmin() => new(seed: true, withAdmin: false);

    public static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var settings = new Dictionary<string, string?>(StringComparer.Ordinal)
        {
            ["DataFile:StoragePath"] = Path.Combine(_root, "Data.txt"),
            ["DataFile:SeedPath"] = _seed ? FixturePath("Data.txt") : string.Empty,
            ["Identity:DbPath"] = Path.Combine(_root, "identity.db"),
            ["ITEMFINDER_ADMIN_EMAIL"] = _withAdmin ? AdminEmail : string.Empty,
            ["ITEMFINDER_ADMIN_PASSWORD"] = _withAdmin ? AdminPassword : string.Empty,
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