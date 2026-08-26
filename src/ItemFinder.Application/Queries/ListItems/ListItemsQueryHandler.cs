using ItemFinder.Application.Dtos;
using ItemFinder.Application.Interfaces;

using ItemFinder.Domain.ValueObjects;

using MediatR;

namespace ItemFinder.Application.Queries.ListItems;

/// <summary>Serves the item list from the managed store; an empty store yields an empty page.</summary>
public sealed class ListItemsQueryHandler(IManagedDataFileStore store)
    : IRequestHandler<ListItemsQuery, ItemListResult>
{
    public Task<ItemListResult> Handle(ListItemsQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        IReadOnlyList<LocatedItem> items = store.CurrentDirectory?.Items ?? [];

        IEnumerable<LocatedItem> filtered = items;
        if (!string.IsNullOrWhiteSpace(request.Search))
        {
            var term = request.Search.Trim();
            filtered = items.Where(item => item.Name.Contains(term, StringComparison.OrdinalIgnoreCase));
        }

        var namesOnly = ListItemsQuery.NameField.Equals(request.Fields, StringComparison.OrdinalIgnoreCase);
        var matches = filtered.ToList();
        var page = matches
            .Skip((request.Page - 1) * request.PageSize)
            .Take(request.PageSize)
            .Select(item => new ItemDto(item.Name, namesOnly ? null : item.Directions))
            .ToList();

        return Task.FromResult(new ItemListResult(page, request.Page, request.PageSize, matches.Count));
    }
}