using System.Text.Json;

using FluentValidation;
using FluentValidation.Results;

using ItemFinder.Api.ExceptionHandling;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;

namespace ItemFinder.Api.Tests;

public sealed class GlobalExceptionHandlerTests
{
    [Fact]
    public async Task TryHandleAsync_ValidationException_Returns400WithFieldErrors()
    {
        var context = CreateContext();
        var failures = new[] { new ValidationFailure("PageSize", "Must be positive.") };

        var handled = await Handler().TryHandleAsync(
            context, new ValidationException(failures), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await ReadBody(context);
        var errors = body.RootElement.GetProperty("errors");
        Assert.Equal("Must be positive.", errors.GetProperty("PageSize")[0].GetString());
    }

    [Fact]
    public async Task TryHandleAsync_BadHttpRequest_ReturnsItsStatusCodeAsProblem()
    {
        var context = CreateContext();

        var handled = await Handler().TryHandleAsync(
            context,
            new BadHttpRequestException("Failed to read the form.", StatusCodes.Status400BadRequest),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, context.Response.StatusCode);
        var body = await ReadBody(context);
        Assert.Equal("Failed to read the form.", body.RootElement.GetProperty("detail").GetString());
    }

    [Fact]
    public async Task TryHandleAsync_UnknownException_Returns500WithoutExceptionDetails()
    {
        var context = CreateContext();

        var handled = await Handler().TryHandleAsync(
            context, new InvalidOperationException("internal secret detail"), CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status500InternalServerError, context.Response.StatusCode);
        var body = await ReadBody(context);
        Assert.DoesNotContain("internal secret detail", body.RootElement.GetRawText());
        Assert.Equal("An unexpected error occurred.", body.RootElement.GetProperty("title").GetString());
    }

    private static GlobalExceptionHandler Handler() =>
        new(NullLogger<GlobalExceptionHandler>.Instance);

    private static DefaultHttpContext CreateContext() =>
        new()
        {
            RequestServices = new ServiceCollection().BuildServiceProvider(),
            Response = { Body = new MemoryStream() },
        };

    private static async Task<JsonDocument> ReadBody(HttpContext context)
    {
        context.Response.Body.Position = 0;
        return await JsonDocument.ParseAsync(context.Response.Body);
    }
}