using ItemFinder.Application.Dtos;
using ItemFinder.Application.Interfaces;

using MediatR;

namespace ItemFinder.Application.Queries.GetItem;

/// <summary>Resolves an item from the managed store's current directory.</summary>
public sealed class GetItemQueryHandler(IManagedDataFileStore store)
    : IRequestHandler<GetItemQuery, ItemDto?>
{
    public Task<ItemDto?> Handle(GetItemQuery request, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);

        var match = store.CurrentDirectory?.Items
            .FirstOrDefault(item => string.Equals(item.Name, request.Name, StringComparison.OrdinalIgnoreCase));

        return Task.FromResult(match is null ? null : new ItemDto(match.Name, match.Directions));
    }
}