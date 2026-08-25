using ItemFinder.Domain.Entities;
using ItemFinder.Domain.ValueObjects;

namespace ItemFinder.Domain.Tests;

public class DirectionForestTests
{
    [Fact]
    public void EnumerateItems_EmptyForest_YieldsNothing()
    {
        var forest = new DirectionForest([]);

        Assert.Empty(forest.EnumerateItems());
    }

    [Fact]
    public void EnumerateItems_SingleRootSingleItem_YieldsItemWithRootDirection()
    {
        var root = new DirectionNode("Walk to the end of the hall.");
        root.AddChild(new ItemNode("Cookies"));
        var forest = new DirectionForest([root]);

        var item = Assert.Single(forest.EnumerateItems());

        Assert.Equal("Cookies", item.Name);
        Assert.Equal(["Walk to the end of the hall."], item.Directions);
    }

    [Fact]
    public void ItemNode_TrimsName()
    {
        var item = new ItemNode("Milk  ");

        Assert.Equal("Milk", item.Name);
    }

    [Fact]
    public void EnumerateItems_SampleTree_YieldsAllItemsInDocumentOrder()
    {
        var forest = BuildSampleForest();

        var names = forest.EnumerateItems().Select(i => i.Name).ToList();

        Assert.Equal(["Cookies", "Coffee Mug", "Milk", "Mobile Phone", "Pencils"], names);
    }

    [Fact]
    public void EnumerateItems_SampleTree_MobilePhoneHasFullDirectionChain()
    {
        var forest = BuildSampleForest();

        var mobilePhone = forest.EnumerateItems().Single(i => i.Name == "Mobile Phone");

        Assert.Equal(
            [
                "Walk to the end of the hall.",
                "Turn right.",
                "Go through the door at the end of the hall.",
                "Look on top of the desk.",
            ],
            mobilePhone.Directions);
    }

    [Fact]
    public void EnumerateItems_DeeplyNestedItem_CarriesEveryAncestorDirection()
    {
        var level1 = new DirectionNode("Enter the building lobby.");
        var level2 = new DirectionNode("Go to the east hallway.");
        var level3 = new DirectionNode("Enter the utility room.");
        var level4 = new DirectionNode("Open the tool chest.");
        var level5 = new DirectionNode("Inspect the upper tray.");
        level5.AddChild(new ItemNode("Allen Wrench Set"));
        level4.AddChild(level5);
        level3.AddChild(level4);
        level2.AddChild(level3);
        level1.AddChild(level2);
        var forest = new DirectionForest([level1]);

        var item = Assert.Single(forest.EnumerateItems());

        Assert.Equal(5, item.Directions.Count);
        Assert.Equal("Inspect the upper tray.", item.Directions[^1]);
    }

    [Fact]
    public void EnumerateItems_MultipleRoots_WalksRootsInOrder()
    {
        var first = new DirectionNode("Enter room A.");
        first.AddChild(new ItemNode("Stapler"));
        var second = new DirectionNode("Enter room B.");
        second.AddChild(new ItemNode("Lamp"));
        var forest = new DirectionForest([first, second]);

        var names = forest.EnumerateItems().Select(i => i.Name).ToList();

        Assert.Equal(["Stapler", "Lamp"], names);
    }

    [Fact]
    public void EnumerateItems_DirectionWithoutItems_ContributesNothing()
    {
        var root = new DirectionNode("Walk to the end of the hall.");
        var deadBranch = new DirectionNode("Open the empty cupboard.");
        root.AddChild(deadBranch);
        var stocked = new DirectionNode("Open the drawer.");
        stocked.AddChild(new ItemNode("Pencils"));
        root.AddChild(stocked);
        var forest = new DirectionForest([root]);

        var item = Assert.Single(forest.EnumerateItems());

        Assert.Equal("Pencils", item.Name);
    }

    [Fact]
    public void EnumerateItems_ExtremelyDeepTree_DoesNotOverflow()
    {
        var root = new DirectionNode("Step 0.");
        var current = root;
        for (var depth = 1; depth < 50_000; depth++)
        {
            var next = new DirectionNode($"Step {depth}.");
            current.AddChild(next);
            current = next;
        }

        current.AddChild(new ItemNode("Needle"));
        var forest = new DirectionForest([root]);

        var item = Assert.Single(forest.EnumerateItems());

        Assert.Equal(50_000, item.Directions.Count);
        Assert.Equal("Step 49999.", item.Directions[^1]);
    }

    private static DirectionForest BuildSampleForest()
    {
        var walk = new DirectionNode("Walk to the end of the hall.");

        var turnLeft = new DirectionNode("Turn left.");
        var firstDoor = new DirectionNode("Go through the first door on the right.");
        var cabinetLeft = new DirectionNode("Open the cabinet on the left.");
        cabinetLeft.AddChild(new ItemNode("Cookies"));
        var cabinetSink = new DirectionNode("Open the cabinet above the sink.");
        cabinetSink.AddChild(new ItemNode("Coffee Mug"));
        var fridge = new DirectionNode("Open the refridgerator.");
        fridge.AddChild(new ItemNode("Milk  "));
        firstDoor.AddChild(cabinetLeft);
        firstDoor.AddChild(cabinetSink);
        firstDoor.AddChild(fridge);
        turnLeft.AddChild(firstDoor);

        var turnRight = new DirectionNode("Turn right.");
        var endDoor = new DirectionNode("Go through the door at the end of the hall.");
        var desk = new DirectionNode("Look on top of the desk.");
        desk.AddChild(new ItemNode("Mobile Phone"));
        var drawer = new DirectionNode("Open the desk drawer.");
        drawer.AddChild(new ItemNode("Pencils"));
        endDoor.AddChild(desk);
        endDoor.AddChild(drawer);
        turnRight.AddChild(endDoor);

        walk.AddChild(turnLeft);
        walk.AddChild(turnRight);
        return new DirectionForest([walk]);
    }
}