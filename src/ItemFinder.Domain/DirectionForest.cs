namespace ItemFinder.Domain;

/// <summary>The parsed data file: one or more root directions and everything beneath them.</summary>
public sealed class DirectionForest(IEnumerable<DirectionNode> roots)
{
    private readonly List<DirectionNode> _roots = [.. roots];

    public IReadOnlyList<DirectionNode> Roots => _roots;

    /// <summary>Yields every item with its full direction chain, in document order.</summary>
    public IEnumerable<LocatedItem> EnumerateItems()
    {
        foreach (var root in _roots)
        {
            foreach (var item in Walk(root, []))
            {
                yield return item;
            }
        }
    }

    private static IEnumerable<LocatedItem> Walk(DirectionNode direction, List<string> pathSoFar)
    {
        pathSoFar.Add(direction.Text);

        foreach (var child in direction.Children)
        {
            switch (child)
            {
                case ItemNode item:
                    yield return new LocatedItem(item.Name, [.. pathSoFar]);
                    break;
                case DirectionNode nested:
                    foreach (var located in Walk(nested, pathSoFar))
                    {
                        yield return located;
                    }

                    break;
            }
        }

        pathSoFar.RemoveAt(pathSoFar.Count - 1);
    }
}