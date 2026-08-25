namespace ItemFinder.Application.Dtos;

/// <summary>One page of the item list plus the paging envelope.</summary>
public sealed record ItemListResult(IReadOnlyList<ItemDto> Items, int Page, int PageSize, int TotalItems);