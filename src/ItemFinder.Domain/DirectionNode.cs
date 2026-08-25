namespace ItemFinder.Domain;

/// <summary>A direction step that may contain further steps or items beneath it.</summary>
public sealed class DirectionNode(string text) : Node
{
    private readonly List<Node> _children = [];

    public string Text { get; } = text;

    public IReadOnlyList<Node> Children => _children;

    public void AddChild(Node child) => _children.Add(child);
}