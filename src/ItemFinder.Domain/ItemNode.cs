namespace ItemFinder.Domain;

/// <summary>A findable item; always a leaf of the hierarchy.</summary>
public sealed class ItemNode(string name) : Node
{
    public string Name { get; } = name.Trim();
}
