using FluentValidation;

using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace ItemFinder.Api.ExceptionHandling;

/// <summary>
/// Maps unhandled exceptions to RFC 7807 responses: validation failures become 400 with
/// per-field errors, everything else a generic 500 that never leaks exception details.
/// </summary>
public sealed partial class GlobalExceptionHandler(ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        ProblemDetails problem = exception switch
        {
            ValidationException validation => new ValidationProblemDetails(ToFieldErrors(validation))
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validation failed.",
            },
            _ => UnexpectedError(httpContext, exception),
        };

        httpContext.Response.StatusCode = problem.Status!.Value;
        await httpContext.Response.WriteAsJsonAsync<object>(
            problem, options: null, contentType: "application/problem+json", cancellationToken);
        return true;
    }

    [LoggerMessage(Level = LogLevel.Error, Message = "Unhandled exception for {Method} {Path}")]
    private static partial void LogUnhandled(ILogger logger, Exception exception, string method, string path);

    private static Dictionary<string, string[]> ToFieldErrors(ValidationException exception) =>
        exception.Errors
            .GroupBy(failure => failure.PropertyName, StringComparer.Ordinal)
            .ToDictionary(
                group => group.Key,
                group => group.Select(failure => failure.ErrorMessage).ToArray(),
                StringComparer.Ordinal);

    private ProblemDetails UnexpectedError(HttpContext httpContext, Exception exception)
    {
        LogUnhandled(logger, exception, httpContext.Request.Method, httpContext.Request.Path);
        return new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred.",
        };
    }
}