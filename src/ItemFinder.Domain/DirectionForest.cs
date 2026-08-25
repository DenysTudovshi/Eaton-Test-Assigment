namespace ItemFinder.Domain;

/// <summary>The parsed data file: one or more root directions and everything beneath them.</summary>
public sealed class DirectionForest(IEnumerable<DirectionNode> roots)
{
    private readonly List<DirectionNode> _roots = [.. roots];

    public IReadOnlyList<DirectionNode> Roots => _roots;

    /// <summary>Yields every item with its full direction chain, in document order.</summary>
    /// <remarks>Iterative traversal: the tree depth comes from the data file, so recursion could exhaust the stack.</remarks>
    public IEnumerable<LocatedItem> EnumerateItems()
    {
        var pending = new Stack<(Node Node, int Depth)>();
        var path = new List<string>();

        for (var i = _roots.Count - 1; i >= 0; i--)
        {
            pending.Push((_roots[i], 0));
        }

        while (pending.Count > 0)
        {
            var (node, depth) = pending.Pop();
            path.RemoveRange(depth, path.Count - depth);

            if (node is ItemNode item)
            {
                yield return new LocatedItem(item.Name, [.. path]);
                continue;
            }

            var direction = (DirectionNode)node;
            path.Add(direction.Text);
            for (var i = direction.Children.Count - 1; i >= 0; i--)
            {
                pending.Push((direction.Children[i], depth + 1));
            }
        }
    }
}