using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

using ItemFinder.Api.Tests.Helpers;

namespace ItemFinder.Api.Tests;

public sealed class SecurityHardeningTests
{
    private const string DataFileUri = "/api/v1/data-file";
    private const string OneItemContent = "+ Room\n└── Item: Lamp";

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task RegisteredNonAdmin_Returns403(string method)
    {
        using var factory = new ApiTestFactory();
        using var client = await ApiClients.CreateUserClient(factory, $"plain-{method}@test.local");
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(DataFileUri, UriKind.Relative));
        if (method == "PUT")
        {
            request.Content = ApiClients.FileUpload(OneItemContent);
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Put_NonTxtExtension_Returns400BeforeParsing()
    {
        using var factory = new ApiTestFactory();
        using var client = await ApiClients.CreateAdminClient(factory);

        using var upload = ApiClients.FileUpload(OneItemContent, fileName: "report.pdf");
        using var response = await client.PutAsync(new Uri(DataFileUri, UriKind.Relative), upload);

        // 400 with a FileName field error proves FluentValidation rejected the upload
        // in the pipeline, before the handler (and therefore the parser) ever ran.
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("FileName", out _));
    }

    [Fact]
    public async Task Put_OversizedFile_Returns400BeforeParsing()
    {
        using var factory = new ApiTestFactory();
        using var client = await ApiClients.CreateAdminClient(factory);
        var oversized = new string('a', (1024 * 1024) + 1);

        using var upload = ApiClients.FileUpload(oversized);
        using var response = await client.PutAsync(new Uri(DataFileUri, UriKind.Relative), upload);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("FileSize", out _));
    }

    [Fact]
    public async Task IdentityEndpoints_BurstOverLimit_Returns429()
    {
        using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var wrongCredentials = new { email = "nobody@test.local", password = "Wrong!Passw0rd" };

        var lastStatus = HttpStatusCode.OK;
        for (var attempt = 0; attempt < 11; attempt++)
        {
            using var response = await client.PostAsJsonAsync("/api/v1/identity/login", wrongCredentials);
            lastStatus = response.StatusCode;
        }

        Assert.Equal(HttpStatusCode.TooManyRequests, lastStatus);
    }

    [Fact]
    public async Task Login_AfterRepeatedFailures_LocksOutEvenWithCorrectPassword()
    {
        using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        var email = "lockout@test.local";
        using var register = await client.PostAsJsonAsync(
            "/api/v1/identity/register", new { email, password = ApiClients.UserPassword });
        Assert.Equal(HttpStatusCode.OK, register.StatusCode);

        for (var attempt = 0; attempt < 5; attempt++)
        {
            using var failed = await client.PostAsJsonAsync(
                "/api/v1/identity/login", new { email, password = "Wrong!Passw0rd" });
            Assert.Equal(HttpStatusCode.Unauthorized, failed.StatusCode);
        }

        using var lockedOut = await client.PostAsJsonAsync(
            "/api/v1/identity/login", new { email, password = ApiClients.UserPassword });
        Assert.Equal(HttpStatusCode.Unauthorized, lockedOut.StatusCode);
    }
}