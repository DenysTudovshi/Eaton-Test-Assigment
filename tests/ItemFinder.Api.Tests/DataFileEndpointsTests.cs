using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;

using ItemFinder.Api.Tests.Helpers;

namespace ItemFinder.Api.Tests;

public sealed class DataFileEndpointsTests
{
    private const string DataFileUri = "/api/v1/data-file";
    private const string OneItemContent = "+ Room\n└── Item: Lamp";

    [Fact]
    public async Task AdminLifecycle_DownloadReplaceDeleteBehavePerContract()
    {
        using var factory = new ApiTestFactory();
        using var client = await CreateAdminClient(factory);

        using (var download = await client.GetAsync(new Uri(DataFileUri, UriKind.Relative)))
        {
            Assert.Equal(HttpStatusCode.OK, download.StatusCode);
            Assert.Equal("text/plain", download.Content.Headers.ContentType?.MediaType);
            Assert.Equal(
                await File.ReadAllTextAsync(ApiTestFactory.FixturePath("Data.txt")),
                await download.Content.ReadAsStringAsync());
        }

        var mediumContent = await File.ReadAllTextAsync(ApiTestFactory.FixturePath("Data-medium.txt"));
        using var mediumUpload = FileUpload(mediumContent);
        using (var replace = await client.PutAsync(new Uri(DataFileUri, UriKind.Relative), mediumUpload))
        {
            Assert.Equal(HttpStatusCode.OK, replace.StatusCode);
            using var body = JsonDocument.Parse(await replace.Content.ReadAsStringAsync());
            Assert.Equal(9, body.RootElement.GetProperty("itemCount").GetInt32());
        }

        Assert.Equal(9, await CountItems(client));

        using (var delete = await client.DeleteAsync(new Uri(DataFileUri, UriKind.Relative)))
        {
            Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
        }

        using (var downloadAfterDelete = await client.GetAsync(new Uri(DataFileUri, UriKind.Relative)))
        {
            Assert.Equal(HttpStatusCode.NotFound, downloadAfterDelete.StatusCode);
        }

        Assert.Equal(0, await CountItems(client));

        using (var deleteAgain = await client.DeleteAsync(new Uri(DataFileUri, UriKind.Relative)))
        {
            Assert.Equal(HttpStatusCode.NoContent, deleteAgain.StatusCode);
        }

        using var oneItemUpload = FileUpload(OneItemContent);
        using (var create = await client.PutAsync(new Uri(DataFileUri, UriKind.Relative), oneItemUpload))
        {
            Assert.Equal(HttpStatusCode.Created, create.StatusCode);
            using var body = JsonDocument.Parse(await create.Content.ReadAsStringAsync());
            Assert.Equal(1, body.RootElement.GetProperty("itemCount").GetInt32());
        }
    }

    [Fact]
    public async Task Put_InvalidFile_Returns422WithParseErrors_AndChangesNothing()
    {
        using var factory = new ApiTestFactory();
        using var client = await CreateAdminClient(factory);

        using var upload = FileUpload("this is not a data file");
        using var response = await client.PutAsync(new Uri(DataFileUri, UriKind.Relative), upload);

        Assert.Equal(HttpStatusCode.UnprocessableEntity, response.StatusCode);
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        var errors = body.RootElement.GetProperty("errors");
        Assert.True(errors.GetArrayLength() > 0);
        Assert.False(string.IsNullOrWhiteSpace(errors[0].GetProperty("kind").GetString()));
        Assert.False(string.IsNullOrWhiteSpace(errors[0].GetProperty("message").GetString()));

        Assert.Equal(5, await CountItems(client));
    }

    [Fact]
    public async Task Put_WithoutFile_Returns400()
    {
        using var factory = new ApiTestFactory();
        using var client = await CreateAdminClient(factory);
        using var empty = new MultipartFormDataContent();

        using var response = await client.PutAsync(new Uri(DataFileUri, UriKind.Relative), empty);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Theory]
    [InlineData("GET")]
    [InlineData("PUT")]
    [InlineData("DELETE")]
    public async Task AnonymousRequest_Returns401(string method)
    {
        using var factory = new ApiTestFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(new HttpMethod(method), new Uri(DataFileUri, UriKind.Relative));
        if (method == "PUT")
        {
            request.Content = FileUpload(OneItemContent);
        }

        using var response = await client.SendAsync(request);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    private static MultipartFormDataContent FileUpload(string content, string fileName = "Data.txt")
    {
        var multipart = new MultipartFormDataContent();
#pragma warning disable CA2000 // the multipart content owns the part and disposes it
        var fileContent = new ByteArrayContent(Encoding.UTF8.GetBytes(content));
#pragma warning restore CA2000
        fileContent.Headers.ContentType = new MediaTypeHeaderValue("text/plain");
        multipart.Add(fileContent, "file", fileName);
        return multipart;
    }

    private static async Task<HttpClient> CreateAdminClient(ApiTestFactory factory)
    {
        var client = factory.CreateClient();
        using var login = await client.PostAsJsonAsync(
            "/api/v1/identity/login",
            new { email = ApiTestFactory.AdminEmail, password = ApiTestFactory.AdminPassword });
        using var body = JsonDocument.Parse(await login.Content.ReadAsStringAsync());
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", body.RootElement.GetProperty("accessToken").GetString());
        return client;
    }

    private static async Task<int> CountItems(HttpClient client)
    {
        using var response = await client.GetAsync(new Uri("/api/v1/items", UriKind.Relative));
        using var body = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return body.RootElement.GetProperty("totalItems").GetInt32();
    }
}