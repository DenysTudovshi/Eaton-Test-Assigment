using ItemFinder.Application.Dtos;
using ItemFinder.Application.Queries.GetItem;
using ItemFinder.Application.Queries.ListItems;

using MediatR;

namespace ItemFinder.Api.Endpoints;

/// <summary>Public, read-only item endpoints served from the managed data file.</summary>
public static class ItemEndpoints
{
    public static IEndpointRouteBuilder MapItemEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup(ApiRoutes.Items).WithTags("Items");

        group.MapGet("/", GetItems)
            .WithName("GetItems")
            .WithSummary("List items alphabetically; optional search filter and paging.")
            .Produces<ItemListResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

        group.MapGet("/{name}", GetItem)
            .WithName("GetItemByName")
            .WithSummary("Fetch one item by exact, case-insensitive name.")
            .Produces<ItemDto>(StatusCodes.Status200OK)
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }

    private static async Task<IResult> GetItems(
        [AsParameters] ListItemsQuery query,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(query, cancellationToken));

    private static async Task<IResult> GetItem(
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