using ItemFinder.Application.Dtos;
using ItemFinder.Application.Queries.GetItem;

using MediatR;

namespace ItemFinder.Api.Endpoints.Items;

/// <summary>One item by exact, case-insensitive name.</summary>
public sealed class GetItemByName : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/{name}", Handle)
            .WithName("GetItemByName")
            .WithSummary("Fetch one item by exact, case-insensitive name.")
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

    private static async Task<IResult> Handle(
        string name,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var item = await sender.Send(new GetItemQuery(name), cancellationToken);
        return item is null
            ? TypedResults.Problem(
                title: $"No item named '{name}' exists.",
                statusCode: StatusCodes.Status404NotFound)
            : TypedResults.Ok(item);
    }
}