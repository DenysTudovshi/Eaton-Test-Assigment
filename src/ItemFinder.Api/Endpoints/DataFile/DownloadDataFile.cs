using System.Text;

using ItemFinder.Application.Queries.GetDataFile;

using MediatR;

namespace ItemFinder.Api.Endpoints.DataFile;

/// <summary>Download the current data file as plain text.</summary>
public sealed class DownloadDataFile : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
            .WithName("DownloadDataFile")
            .WithSummary("Download the current data file.")
            .Produces(StatusCodes.Status200OK, contentType: "text/plain")
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        var content = await sender.Send(new GetDataFileQuery(), cancellationToken);
        return content is null
            ? TypedResults.Problem(title: "No data file is stored.", statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Text(content, "text/plain", Encoding.UTF8);
    }
}