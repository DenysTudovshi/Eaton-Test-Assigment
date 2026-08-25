using System.Text;

using ItemFinder.Application;
using ItemFinder.Infrastructure;

namespace ItemFinder.Infrastructure.Tests;

public class DataFileParserDepthTests
{
    private readonly DataFileParser _parser = new();

    [Fact]
    public void ParseText_HundredLevelChain_ResolvesTheFullChain()
    {
        const int levels = 100;
        var text = new StringBuilder("+ Step 1.\n");
        for (var depth = 2; depth <= levels; depth++)
        {
            text.Append(Indent(depth - 2)).Append("└──+ Step ").Append(depth).Append(".\n");
        }

        text.Append(Indent(levels - 1)).Append("└── Item: Needle");

        var result = _parser.ParseText(text.ToString());

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var item = Assert.Single(new ItemDirectory(result.Forest!).Items);
        Assert.Equal(levels, item.Directions.Count);
        Assert.Equal("Step 1.", item.Directions[0]);
        Assert.Equal($"Step {levels}.", item.Directions[^1]);
    }

    [Fact]
    public void ParseText_ItemAtEveryLevel_EachCarriesItsOwnChainLength()
    {
        const int levels = 10;
        var text = new StringBuilder("+ Level 1.\n");
        for (var depth = 1; depth < levels; depth++)
        {
            text.Append(Indent(depth - 1)).Append("├── Item: Item ").Append(depth).Append('\n');
            text.Append(Indent(depth - 1)).Append("└──+ Level ").Append(depth + 1).Append(".\n");
        }

        text.Append(Indent(levels - 1)).Append("└── Item: Item ").Append(levels);

        var result = _parser.ParseText(text.ToString());

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(levels, directory.Items.Count);
        for (var n = 1; n <= levels; n++)
        {
            var directions = directory.GetDirections($"Item {n}")!;
            Assert.Equal(n, directions.Count);
            Assert.Equal($"Level {n}.", directions[^1]);
        }
    }

    [Fact]
    public void ParseText_DeepBranchThenShallowSibling_BothChainsAreCorrect()
    {
        const string text = """
            + Enter the warehouse.
            ├──+ Climb to the mezzanine.
            |  └──+ Open the archive room.
            |     └──+ Search the last cabinet.
            |        └── Item: Blueprints
            └──+ Check the loading dock.
               └── Item: Hand Truck
            """;

        var result = _parser.ParseText(text);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(
            [
                "Enter the warehouse.",
                "Climb to the mezzanine.",
                "Open the archive room.",
                "Search the last cabinet.",
            ],
            directory.GetDirections("Blueprints"));
        Assert.Equal(
            ["Enter the warehouse.", "Check the loading dock."],
            directory.GetDirections("Hand Truck"));
    }

    private static string Indent(int groups) => string.Concat(Enumerable.Repeat("   ", groups));
}
