using ItemFinder.Application.Commands.DeleteDataFile;

using MediatR;

namespace ItemFinder.Api.Endpoints.DataFile;

/// <summary>Remove the data file; idempotent, and durable across restarts.</summary>
public sealed class RemoveDataFile : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapDelete("/", Handle)
            .WithName("DeleteDataFile")
            .WithSummary("Remove the data file; idempotent.")
            .Produces(StatusCodes.Status204NoContent);

    private static async Task<IResult> Handle(ISender sender, CancellationToken cancellationToken)
    {
        await sender.Send(new DeleteDataFileCommand(), cancellationToken);
        return TypedResults.NoContent();
    }
}