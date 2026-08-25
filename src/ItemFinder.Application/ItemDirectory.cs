using ItemFinder.Domain;

namespace ItemFinder.Application;

/// <summary>Lookup over a parsed forest: the alphabetical item list and per-item directions.</summary>
public sealed class ItemDirectory
{
    private readonly Dictionary<string, IReadOnlyList<string>> _directionsByName;

    public ItemDirectory(DirectionForest forest)
    {
        _directionsByName = [];
        foreach (var item in forest.EnumerateItems())
        {
            _directionsByName[item.Name] = item.Directions;
        }

        AvailableItems = _directionsByName.Keys
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    /// <summary>Item names in alphabetical order (case-insensitive).</summary>
    public IReadOnlyList<string> AvailableItems { get; }

    /// <summary>The direction steps to <paramref name="itemName"/>, or null when no such item exists.</summary>
    public IReadOnlyList<string>? GetDirections(string itemName) =>
        _directionsByName.TryGetValue(itemName, out var directions) ? directions : null;
}