using System.Text;

using ItemFinder.Application.Commands.ReplaceDataFile;
using ItemFinder.Application.Dtos;
using ItemFinder.Application.Options;

using MediatR;

using Microsoft.Extensions.Options;

namespace ItemFinder.Api.Endpoints.DataFile;

/// <summary>
/// Upload a replacement data file. The grammar parser gates the content before anything
/// is stored; PUT answers 201 the first time and 200 on replacement.
/// </summary>
public sealed class UploadDataFile : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapPut("/", Handle)
            .WithName("ReplaceDataFile")
            .WithSummary("Upload a data file, replacing the current one after grammar validation.")
            // Clients authenticate with bearer tokens, not cookies, so CSRF does not apply to this form post.
            .DisableAntiforgery()
            .Produces<DataFileUploadResponse>(StatusCodes.Status200OK)
            .Produces<DataFileUploadResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status422UnprocessableEntity);

    private static async Task<IResult> Handle(
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
}