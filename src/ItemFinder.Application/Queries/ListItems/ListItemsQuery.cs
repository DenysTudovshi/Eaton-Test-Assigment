using ItemFinder.Application.Dtos;

using MediatR;

namespace ItemFinder.Application.Queries.ListItems;

/// <summary>Lists items alphabetically, optionally filtered by a case-insensitive substring.</summary>
public sealed record ListItemsQuery(string? Search = null, int Page = 1, int PageSize = 50)
    : IRequest<ItemListResult>;