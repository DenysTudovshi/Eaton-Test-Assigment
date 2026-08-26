using ItemFinder.Application.Dtos;

using MediatR;

namespace ItemFinder.Application.Queries.ListItems;

/// <summary>
/// Lists items alphabetically. <paramref name="Search"/> filters by case-insensitive
/// substring; repeated <paramref name="Name"/> values filter to those exact names
/// (batch direction lookup); <paramref name="Fields"/> set to "name" projects to names only.
/// </summary>
public sealed record ListItemsQuery(
    string? Search = null,
#pragma warning disable CA1819 // minimal-API binding of repeated query params requires a concrete array
    string[]? Name = null,
#pragma warning restore CA1819
    string? Fields = null,
    int Page = 1,
    int PageSize = 50) : IRequest<ItemListResult>
{
    /// <summary>The only supported projection value for <see cref="Fields"/>.</summary>
    public const string NameField = "name";
}