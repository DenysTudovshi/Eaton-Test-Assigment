using System.Text;

using ItemFinder.Api.Identity;
using ItemFinder.Application.Commands.DeleteDataFile;
using ItemFinder.Application.Commands.ReplaceDataFile;
using ItemFinder.Application.Dtos;
using ItemFinder.Application.Options;
using ItemFinder.Application.Queries.GetDataFile;

using MediatR;

using Microsoft.Extensions.Options;

namespace ItemFinder.Api.Endpoints;

/// <summary>
/// The managed data file as a singleton resource, restricted to the Admin role.
/// PUT is an idempotent replace; DELETE succeeds even when nothing is stored.
/// </summary>
public static class DataFileEndpoints
{
    public static IEndpointRouteBuilder MapDataFileEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.DataFile)
            .WithTags("Data file")
            .RequireAuthorization(policy => policy.RequireRole(IdentitySeeder.AdminRole));

        group.MapGet("/", GetDataFile)
            .WithName("DownloadDataFile")
            .WithSummary("Download the current data file.")
            .Produces(StatusCodes.Status200OK, contentType: "text/plain")
            .ProducesProblem(StatusCodes.Status404NotFound);

        group.MapPut("/", ReplaceDataFile)
            .WithName("ReplaceDataFile")
            .WithSummary("Upload a data file, replacing the current one after grammar validation.")
            // Clients authenticate with bearer tokens, not cookies, so CSRF does not apply to this form post.
            .DisableAntiforgery()
            .Produces<DataFileUploadResponse>(StatusCodes.Status200OK)
            .Produces<DataFileUploadResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

        group.MapDelete("/", DeleteDataFile)
            .WithName("DeleteDataFile")
            .WithSummary("Remove the data file; idempotent.")
            .Produces(StatusCodes.Status204NoContent);

        return app;
    }

    private static async Task<IResult> GetDataFile(ISender sender, CancellationToken cancellationToken)
    {
        var content = await sender.Send(new GetDataFileQuery(), cancellationToken);
        return content is null
            ? TypedResults.Problem(title: "No data file is stored.", statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Text(content, "text/plain", Encoding.UTF8);
    }

    private static async Task<IResult> ReplaceDataFile(
        IFormFile? file,
        ISender sender,
        IOptions<DataFileOptions> dataFileOptions,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(dataFileOptions);

        if (file is null || file.Length == 0)
        {
            return TypedResults.Problem(
                title: "Attach the data file in the 'file' form field.",
                statusCode: StatusCodes.Status400BadRequest);
        }

        // Checked from the declared length so an oversized body is never read into
        // memory; the command validator re-checks the same limit in the pipeline.
        var maxSizeBytes = dataFileOptions.Value.MaxSizeBytes;
        if (file.Length > maxSizeBytes)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>(StringComparer.Ordinal)
            {
                ["FileSize"] = [$"The file exceeds the {maxSizeBytes / 1024} KB size limit."],
            });
        }

        string content;
        using (var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8))
        {
            content = await reader.ReadToEndAsync(cancellationToken);
        }

        var result = await sender.Send(
            new ReplaceDataFileCommand(content, file.FileName, file.Length), cancellationToken);

        if (!result.Success)
        {
            return TypedResults.Problem(
                title: "The uploaded file is not a valid data file.",
                statusCode: StatusCodes.Status422UnprocessableEntity,
                extensions: new Dictionary<string, object?>(StringComparer.Ordinal)
                {
                    ["errors"] = result.Errors
                        .Select(error => new
                        {
                            kind = error.Kind.ToString(),
                            line = error.LineNumber,
                            message = error.Message,
                        })
                        .ToList(),
                });
        }

        var response = new DataFileUploadResponse(result.ItemCount);
        return result.CreatedNew
            ? TypedResults.Created(ApiRoutes.DataFile, response)
            : TypedResults.Ok(response);
    }

    private static async Task<IResult> DeleteDataFile(ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDataFileCommand(), cancellationToken);
        return TypedResults.NoContent();
    }
}