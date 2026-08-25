namespace ItemFinder.Domain;

/// <summary>An item together with the direction steps leading to it, root first.</summary>
public sealed record LocatedItem(string Name, IReadOnlyList<string> Directions);
