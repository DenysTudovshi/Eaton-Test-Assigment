using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

namespace ItemFinder.Api.Tests.Helpers;

/// <summary>HTTP helpers shared by the endpoint test suites.</summary>
internal static class ApiClients
{
    public const string UserPassword = "User!Passw0rd";

    /// <summary>A client authenticated as the seeded admin.</summary>
    public static Task<HttpClient> CreateAdminClient(ApiTestFactory factory) =>
        CreateAuthenticatedClient(factory, ApiTestFactory.AdminEmail, ApiTestFactory.AdminPassword, register: false);

    /// <summary>A client authenticated as a freshly registered, role-less user.</summary>
    public static Task<HttpClient> CreateUserClient(ApiTestFactory factory, string email) =>
        CreateAuthenticatedClient(factory, email, UserPassword, register: true);

    public static MultipartFormDataContent FileUpload(string content, string fileName = "Data.txt")
    {
        var multipart = new MultipartFormDataContent();
#pragma warning disable CA2000 // the multipart content owns the part and disposes it
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
#pragma warning restore CA2000
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(fileContent, "file", fileName);
        return multipart;
    }

    private static async Task<HttpClient> CreateAuthenticatedClient(
        ApiTestFactory factory, string email, string password, bool register)
    {
        var client = factory.CreateClient();
        var credentials = new { email, password };

        if (register)
        {
            using var registerResponse = await client.PostAsJsonAsync("/api/v1/identity/register", credentials);
        }

        using var login = await client.PostAsJsonAsync("/api/v1/identity/login", credentials);
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", body.RootElement.GetProperty("accessToken").GetString());
        return client;
    }
}