using System.Net;
using System.Text.Json;

using ItemFinder.Api.Tests.Helpers;

namespace ItemFinder.Api.Tests;

public sealed class ItemEndpointsTests : IClassFixture<ApiTestFactory>
{
    private readonly ApiTestFactory _factory;

    public ItemEndpointsTests(ApiTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetItems_ReturnsAlphabeticalItemsWithDirections()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal(5, body.RootElement.GetProperty("totalItems").GetInt32());

        var items = body.RootElement.GetProperty("items");
        Assert.Equal("Coffee Mug", items[0].GetProperty("name").GetString());
        Assert.Equal("Mobile Phone", items[3].GetProperty("name").GetString());

        var directions = items[3].GetProperty("directions");
        Assert.Equal(4, directions.GetArrayLength());
        Assert.Equal("Walk to the end of the hall.", directions[0].GetString());
        Assert.Equal("Look on top of the desk.", directions[3].GetString());
    }

    [Fact]
    public async Task GetItems_Search_FiltersCaseInsensitively()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items?search=mug", UriKind.Relative));

        using var body = await ReadJson(response);
        Assert.Equal(1, body.RootElement.GetProperty("totalItems").GetInt32());
        var item = body.RootElement.GetProperty("items")[0];
        Assert.Equal("Coffee Mug", item.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetItems_Paging_ReturnsRequestedSlice()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items?page=2&pageSize=2", UriKind.Relative));

        using var body = await ReadJson(response);
        Assert.Equal(2, body.RootElement.GetProperty("page").GetInt32());
        Assert.Equal(2, body.RootElement.GetProperty("pageSize").GetInt32());
        Assert.Equal(5, body.RootElement.GetProperty("totalItems").GetInt32());

        var items = body.RootElement.GetProperty("items");
        Assert.Equal(2, items.GetArrayLength());
        Assert.Equal("Milk", items[0].GetProperty("name").GetString());
        Assert.Equal("Mobile Phone", items[1].GetProperty("name").GetString());
    }

    [Theory]
    [InlineData("page=0", "Page")]
    [InlineData("pageSize=0", "PageSize")]
    [InlineData("pageSize=201", "PageSize")]
    public async Task GetItems_InvalidPaging_Returns400WithFieldError(string query, string field)
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri($"/api/v1/items?{query}", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty(field, out _));
    }

    [Fact]
    public async Task GetItem_KnownName_ReturnsDirections()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items/Mobile%20Phone", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal("Mobile Phone", body.RootElement.GetProperty("name").GetString());
        Assert.Equal(
            "Go through the door at the end of the hall.",
            body.RootElement.GetProperty("directions")[2].GetString());
    }

    [Fact]
    public async Task GetItem_MatchesCaseInsensitively()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items/mobile%20phone", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal("Mobile Phone", body.RootElement.GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetItem_Unknown_Returns404Problem()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items/No%20Such%20Item", UriKind.Relative));

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task GetItems_EmptyStore_ReturnsEmptyPage()
    {
        using var factory = ApiTestFactory.Unseeded();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal(0, body.RootElement.GetProperty("totalItems").GetInt32());
        Assert.Equal(0, body.RootElement.GetProperty("items").GetArrayLength());
    }

    [Fact]
    public async Task GetItems_FieldsName_ReturnsNamesWithoutDirections()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items?fields=name", UriKind.Relative));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.Equal(5, body.RootElement.GetProperty("totalItems").GetInt32());

        var first = body.RootElement.GetProperty("items")[0];
        Assert.Equal("Coffee Mug", first.GetProperty("name").GetString());
        Assert.False(first.TryGetProperty("directions", out _));
    }

    [Fact]
    public async Task GetItems_FieldsName_CombinesWithSearch()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items?fields=name&search=co", UriKind.Relative));

        using var body = await ReadJson(response);
        Assert.Equal(2, body.RootElement.GetProperty("totalItems").GetInt32());
        Assert.Equal("Coffee Mug", body.RootElement.GetProperty("items")[0].GetProperty("name").GetString());
        Assert.Equal("Cookies", body.RootElement.GetProperty("items")[1].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetItems_UnknownFieldsValue_Returns400()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/v1/items?fields=everything", UriKind.Relative));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var body = await ReadJson(response);
        Assert.True(body.RootElement.GetProperty("errors").TryGetProperty("Fields", out _));
    }

    private static async Task<JsonDocument> ReadJson(HttpResponseMessage response) =>
        JsonDocument.Parse(await response.Content.ReadAsStringAsync());
}