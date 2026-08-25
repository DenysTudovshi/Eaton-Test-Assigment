using ItemFinder.Domain;

namespace ItemFinder.Application;

/// <summary>Lookup over a parsed forest: the alphabetical item list and per-item directions.</summary>
public sealed class ItemDirectory
{
    private readonly Dictionary<string, IReadOnlyList<string>> _directionsByName;

    public ItemDirectory(DirectionForest forest)
    {
        Items = forest.EnumerateItems()
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        _directionsByName = [];
        foreach (var item in Items)
        {
            _directionsByName[item.Name] = item.Directions;
        }
    }

    /// <summary>Every item with its directions, alphabetical by name (case-insensitive).</summary>
    public IReadOnlyList<LocatedItem> Items { get; }

    /// <summary>The direction steps to <paramref name="itemName"/>, or null when no such item exists.</summary>
    public IReadOnlyList<string>? GetDirections(string itemName) =>
        _directionsByName.TryGetValue(itemName, out var directions) ? directions : null;
}