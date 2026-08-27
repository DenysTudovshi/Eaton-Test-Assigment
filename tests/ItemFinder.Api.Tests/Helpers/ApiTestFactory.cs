using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace ItemFinder.Api.Tests.Helpers;

/// <summary>
/// Hosts the API over an isolated temp data-file store and identity database.
/// Seeded with the canonical data file and a configured admin unless disabled.
/// <see cref="WithState"/> hosts over a caller-owned root instead, so tests can
/// simulate consecutive runs of a persistent deployment.
/// </summary>
public sealed class ApiTestFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin!Passw0rd";

    private readonly string _root;
    private readonly bool _ownsRoot;
    private readonly bool _seed;
    private readonly string _adminEmail;
    private readonly string _adminPassword;

    public ApiTestFactory()
        : this(seed: true, withAdmin: true)
    {
    }

    private ApiTestFactory(bool seed, bool withAdmin)
        : this(
            seed,
            NewStateRoot(),
            ownsRoot: true,
            withAdmin ? AdminEmail : string.Empty,
            withAdmin ? AdminPassword : string.Empty)
    {
    }

    private ApiTestFactory(bool seed, string root, bool ownsRoot, string adminEmail, string adminPassword)
    {
        _seed = seed;
        _root = root;
        _ownsRoot = ownsRoot;
        _adminEmail = adminEmail;
        _adminPassword = adminPassword;
    }

    /// <summary>A factory whose data-file store starts empty instead of seeded.</summary>
    public static ApiTestFactory Unseeded() => new(seed: false, withAdmin: true);

    /// <summary>A factory with no admin credentials configured, so no admin is seeded.</summary>
    public static ApiTestFactory WithoutAdmin() => new(seed: true, withAdmin: false);

    /// <summary>
    /// A factory over a caller-owned state root with explicit admin credentials —
    /// one "run" of a persistent deployment. The caller deletes the root when done.
    /// </summary>
    public static ApiTestFactory WithState(string stateRoot, string adminEmail, string adminPassword) =>
        new(seed: true, stateRoot, ownsRoot: false, adminEmail, adminPassword);

    public static string NewStateRoot() =>
        Path.Combine(Path.GetTempPath(), "ItemFinderApiTests", Guid.NewGuid().ToString("N"));

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
            ["ITEMFINDER_ADMIN_EMAIL"] = _adminEmail,
            ["ITEMFINDER_ADMIN_PASSWORD"] = _adminPassword,
        };

        builder.ConfigureAppConfiguration((_, configuration) => configuration.AddInMemoryCollection(settings));
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (!_ownsRoot)
        {
            return;
        }

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