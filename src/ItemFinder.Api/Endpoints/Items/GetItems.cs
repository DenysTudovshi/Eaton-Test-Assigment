using ItemFinder.Application.Dtos;
using ItemFinder.Application.Queries.ListItems;

using MediatR;

namespace ItemFinder.Api.Endpoints.Items;

/// <summary>The item list: alphabetical, searchable, pageable, projectable to names only.</summary>
public sealed class GetItems : IEndpoint
{
    public static void Map(IEndpointRouteBuilder app) =>
        app.MapGet("/", Handle)
            .WithName("GetItems")
            .WithSummary("List items alphabetically; optional search filter, exact-name filters, and paging.")
            .Produces<ItemListResult>(StatusCodes.Status200OK)
            .ProducesValidationProblem();

    private static async Task<IResult> Handle(
        [AsParameters] ListItemsQuery query,
        ISender sender,
        CancellationToken cancellationToken) =>
        TypedResults.Ok(await sender.Send(query, cancellationToken));
}