using System.Net;
using System.Text.Json;

using Microsoft.AspNetCore.Mvc.Testing;

namespace ItemFinder.Api.Tests;

public sealed class SwaggerSmokeTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public SwaggerSmokeTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SwaggerDocument_Get_ReturnsOk()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task SwaggerDocument_IdentitySurface_IsRegisterAndLoginOnly()
    {
        using var client = _factory.CreateClient();

        var json = await client.GetStringAsync(new Uri("/swagger/v1/swagger.json", UriKind.Relative));

        using var document = JsonDocument.Parse(json);
        var identityPaths = document.RootElement.GetProperty("paths").EnumerateObject()
            .Where(path => path.Name.StartsWith("/api/v1/identity", StringComparison.Ordinal))
            .Select(path => path.Name)
            .ToList();
        Assert.Equal(2, identityPaths.Count);
        Assert.Contains("/api/v1/identity/register", identityPaths);
        Assert.Contains("/api/v1/identity/login", identityPaths);
    }
}