namespace ItemFinder.Application.Dtos;

/// <summary>
/// An item as served by the API. Directions are null (and omitted from JSON) when the
/// caller asked for the names-only projection.
/// </summary>
public sealed record ItemDto(string Name, IReadOnlyList<string>? Directions);