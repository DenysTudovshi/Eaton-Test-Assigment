namespace ItemFinder.Application.Dtos;

/// <summary>An item with the direction steps leading to it, as served by the API.</summary>
public sealed record ItemDto(string Name, IReadOnlyList<string> Directions);