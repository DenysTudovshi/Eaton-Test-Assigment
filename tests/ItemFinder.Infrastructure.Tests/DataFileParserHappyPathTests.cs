using ItemFinder.Application.Enums;
using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Results;
using ItemFinder.Application.Services;
using ItemFinder.Infrastructure.Parsing;

namespace ItemFinder.Infrastructure.Tests;

public class DataFileParserHappyPathTests
{
    private readonly DataFileParser _parser = new();

    private static string Fixture(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);

    [Fact]
    public void ParseFile_SampleFile_ListsAllFiveItemsAlphabetically()
    {
        var result = _parser.ParseFile(Fixture("Data.txt"));

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(
            ["Coffee Mug", "Cookies", "Milk", "Mobile Phone", "Pencils"],
            directory.Items.Select(item => item.Name));
    }

    [Fact]
    public void ParseFile_SampleFile_MobilePhoneHasTheWorkedExampleDirections()
    {
        var result = _parser.ParseFile(Fixture("Data.txt"));

        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(
            [
                "Walk to the end of the hall.",
                "Turn right.",
                "Go through the door at the end of the hall.",
                "Look on top of the desk.",
            ],
            directory.GetDirections("Mobile Phone"));
    }

    [Fact]
    public void ParseFile_SampleFile_TrimsItemNames()
    {
        var result = _parser.ParseFile(Fixture("Data.txt"));

        var directory = new ItemDirectory(result.Forest!);
        Assert.NotNull(directory.GetDirections("Milk"));
    }

    [Fact]
    public void ParseFile_MediumFile_ListsAllNineItems()
    {
        var result = _parser.ParseFile(Fixture("Data-medium.txt"));

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(
            [
                "Allen Wrench Set",
                "Envelope Pack",
                "Flashlight",
                "Granola Bars",
                "Notepad",
                "Orange Juice",
                "Shoe Box",
                "Travel Backpack",
                "Work Gloves",
            ],
            directory.Items.Select(item => item.Name));
    }

    [Fact]
    public void ParseFile_MediumFile_FlashlightDirectionsAreCorrect()
    {
        var result = _parser.ParseFile(Fixture("Data-medium.txt"));

        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(
            [
                "Enter the building lobby.",
                "Go to the east hallway.",
                "Enter the utility room.",
                "Check the wall shelf.",
            ],
            directory.GetDirections("Flashlight"));
    }

    [Fact]
    public void ParseFile_MediumFile_DeepestItemsResolveThroughFiveLevels()
    {
        var result = _parser.ParseFile(Fixture("Data-medium.txt"));

        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(
            [
                "Enter the building lobby.",
                "Go to the east hallway.",
                "Enter the utility room.",
                "Open the tool chest.",
                "Inspect the upper tray.",
            ],
            directory.GetDirections("Allen Wrench Set"));
        Assert.Equal("Inspect the lower drawer.", directory.GetDirections("Work Gloves")![^1]);
    }

    [Fact]
    public void ParseText_MinimalHierarchy_Parses()
    {
        const string text = """
            + Enter the room.
            ├──+ Look at the shelf.
            |  └── Item: Book
            └──+ Open the box.
               └── Item: Marble
            """;

        var result = _parser.ParseText(text);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(["Book", "Marble"], directory.Items.Select(item => item.Name));
        Assert.Equal(["Enter the room.", "Open the box."], directory.GetDirections("Marble"));
    }

    [Fact]
    public void ParseText_MultipleRoots_AllContributeItems()
    {
        const string text = """
            + Enter building A.
            └──+ Check the desk.
               └── Item: Keys
            + Enter building B.
            └──+ Check the locker.
               └── Item: Badge
            """;

        var result = _parser.ParseText(text);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
        var directory = new ItemDirectory(result.Forest!);
        Assert.Equal(["Badge", "Keys"], directory.Items.Select(item => item.Name));
        Assert.Equal(["Enter building B.", "Check the locker."], directory.GetDirections("Badge"));
    }
}