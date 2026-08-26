using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

using ItemFinder.Api.Identity;
using ItemFinder.Api.Tests.Helpers;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace ItemFinder.Api.Tests;

public sealed class IdentityEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public IdentityEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ThenLogin_ReturnsBearerToken()
    {
        using var client = _factory.CreateClient();
        var credentials = new { email = "user1@test.local", password = "User!Passw0rd" };

        using var registerResponse = await client.PostAsJsonAsync("/api/v1/identity/register", credentials);
        Assert.Equal(HttpStatusCode.OK, registerResponse.StatusCode);

        using var loginResponse = await client.PostAsJsonAsync("/api/v1/identity/login", credentials);
        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);

        using var body = JsonDocument.Parse(await loginResponse.Content.ReadAsStringAsync());
        Assert.Equal("Bearer", body.RootElement.GetProperty("tokenType").GetString());
        Assert.False(string.IsNullOrWhiteSpace(body.RootElement.GetProperty("accessToken").GetString()));
    }

    [Fact]
    public async Task BearerToken_Authenticates_ProtectedEndpointDistinguishes401From403()
    {
        using var client = _factory.CreateClient();
        var dataFileUri = new Uri("/api/v1/data-file", UriKind.Relative);

        using var anonymous = await client.GetAsync(dataFileUri);
        Assert.Equal(HttpStatusCode.Unauthorized, anonymous.StatusCode);

        var token = await RegisterAndLogin(client, "user2@test.local");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        using var authenticated = await client.GetAsync(dataFileUri);
        Assert.Equal(HttpStatusCode.Forbidden, authenticated.StatusCode);
    }

    [Theory]
    [InlineData("POST", "/api/v1/identity/refresh")]
    [InlineData("GET", "/api/v1/identity/manage/info")]
    [InlineData("POST", "/api/v1/identity/forgotPassword")]
    [InlineData("POST", "/api/v1/identity/resetPassword")]
    public async Task FrameworkIdentityEndpoints_AreRemoved(string method, string route)
    {
        using var client = _factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(route, UriKind.Relative));
        if (method == "POST")
        {
            request.Content = JsonContent.Create(new { });
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Register_InvalidEmail_Returns400WithFieldErrors()
    {
        using var client = _factory.CreateClient();

        using var response = await client.PostAsJsonAsync(
            "/api/v1/identity/register", new { email = "not-an-email", password = "User!Passw0rd" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.TryGetProperty("errors", out var errors));
        Assert.True(errors.EnumerateObject().Any());
    }

    [Fact]
    public async Task Login_WrongPassword_Returns401()
    {
        using var client = _factory.CreateClient();
        await client.PostAsJsonAsync(
            "/api/v1/identity/register", new { email = "user3@test.local", password = "User!Passw0rd" });

        using var response = await client.PostAsJsonAsync(
            "/api/v1/identity/login", new { email = "user3@test.local", password = "Wrong!Passw0rd" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SeededAdmin_ExistsInAdminRole()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();

        var admin = await userManager.FindByEmailAsync(ApiTestFactory.AdminEmail);

        Assert.NotNull(admin);
        Assert.True(await userManager.IsInRoleAsync(admin, "Admin"));
    }

    [Fact]
    public async Task MissingAdminConfig_StillServes_WithoutSeeding()
    {
        using var factory = ApiTestFactory.WithoutAdmin();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items", UriKind.Relative));
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApiUser>>();
        Assert.Null(await userManager.FindByEmailAsync(ApiTestFactory.AdminEmail));
    }

    private static async Task<string> RegisterAndLogin(HttpClient client, string email)
    {
        var credentials = new { email, password = "User!Passw0rd" };
        using var register = await client.PostAsJsonAsync("/api/v1/identity/register", credentials);
        using var login = await client.PostAsJsonAsync("/api/v1/identity/login", credentials);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("accessToken").GetString()!;
    }
}