using ItemFinder.Application;
using ItemFinder.Domain;

namespace ItemFinder.Application.Tests;

public class ItemDirectoryTests
{
    [Fact]
    public void Items_AreSortedAlphabetically()
    {
        var directory = new ItemDirectory(ForestWithItems("Pencils", "Cookies", "Milk", "Mobile Phone", "Coffee Mug"));

        Assert.Equal(
            ["Coffee Mug", "Cookies", "Milk", "Mobile Phone", "Pencils"],
            directory.Items.Select(item => item.Name));
    }

    [Fact]
    public void Items_SortIsCaseInsensitive()
    {
        var directory = new ItemDirectory(ForestWithItems("cherry", "Apple", "banana"));

        Assert.Equal(["Apple", "banana", "cherry"], directory.Items.Select(item => item.Name));
    }

    [Fact]
    public void Items_EmptyForest_IsEmpty()
    {
        var directory = new ItemDirectory(new DirectionForest([]));

        Assert.Empty(directory.Items);
    }

    [Fact]
    public void Items_CarryTheirDirections()
    {
        var directory = new ItemDirectory(ForestWithItems("Cookies"));

        var item = Assert.Single(directory.Items);
        Assert.Equal("Cookies", item.Name);
        Assert.Equal(["Walk in.", "Approach the Cookies."], item.Directions);
    }

    [Fact]
    public void GetDirections_KnownItem_ReturnsFullChain()
    {
        var hall = new DirectionNode("Walk to the end of the hall.");
        var desk = new DirectionNode("Look on top of the desk.");
        desk.AddChild(new ItemNode("Mobile Phone"));
        hall.AddChild(desk);
        var directory = new ItemDirectory(new DirectionForest([hall]));

        var directions = directory.GetDirections("Mobile Phone");

        Assert.NotNull(directions);
        Assert.Equal(["Walk to the end of the hall.", "Look on top of the desk."], directions);
    }

    [Fact]
    public void GetDirections_UnknownItem_ReturnsNull()
    {
        var directory = new ItemDirectory(ForestWithItems("Cookies"));

        Assert.Null(directory.GetDirections("Unicorn"));
    }

    private static DirectionForest ForestWithItems(params string[] names)
    {
        var root = new DirectionNode("Walk in.");
        foreach (var name in names)
        {
            var step = new DirectionNode($"Approach the {name}.");
            step.AddChild(new ItemNode(name));
            root.AddChild(step);
        }

        return new DirectionForest([root]);
    }
}