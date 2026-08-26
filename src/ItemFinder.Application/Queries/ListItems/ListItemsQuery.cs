using ItemFinder.Application.Dtos;

using MediatR;

namespace ItemFinder.Application.Queries.ListItems;

/// <summary>
/// Lists items alphabetically. <paramref name="Search"/> filters by case-insensitive
/// substring; <paramref name="Fields"/> set to "name" projects to names only.
/// </summary>
public sealed record ListItemsQuery(
    string? Search = null,
    string? Fields = null,
    int Page = 1,
    int PageSize = 50) : IRequest<ItemListResult>
{
    /// <summary>The only supported projection value for <see cref="Fields"/>.</summary>
    public const string NameField = "name";
}