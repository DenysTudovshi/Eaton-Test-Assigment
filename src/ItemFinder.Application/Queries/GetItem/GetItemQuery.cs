using ItemFinder.Application.Dtos;

using MediatR;

namespace ItemFinder.Application.Queries.GetItem;

/// <summary>Fetches one item by exact, case-insensitive name; null when no such item exists.</summary>
public sealed record GetItemQuery(string Name) : IRequest<ItemDto?>;