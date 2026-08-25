using System.Net;

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
}