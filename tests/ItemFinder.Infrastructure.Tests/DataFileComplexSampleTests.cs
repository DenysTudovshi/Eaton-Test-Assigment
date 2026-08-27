using ItemFinder.Application.Services;
using ItemFinder.Infrastructure.Parsing;

namespace ItemFinder.Infrastructure.Tests;

/// <summary>
/// Pins the shape of the large sample: three roots, nesting to depth six, an item
/// directly under a root, deep-branch backtracking, and dead branches.
/// </summary>
public sealed class DataFileComplexSampleTests
{
    private readonly DataFileParser _parser = new();

    [Fact]
    public void ComplexSample_Parses_WithTwentyTwoItemsAlphabetical()
    {
        var result = _parser.ParseFile(FixturePath("Data-complex.txt"));

        Assert.True(result.Success);
        var directory = new ItemDirectory(result.Forest);
        Assert.Equal(22, directory.Items.Count);
        Assert.Equal("Conference Phone", directory.Items[0].Name);
        Assert.Equal("Work Lamp", directory.Items[^1].Name);
    }

    [Fact]
    public void DeepestItem_CarriesItsFullDirectionChain()
    {
        var result = _parser.ParseFile(FixturePath("Data-complex.txt"));

        Assert.True(result.Success);
        var directory = new ItemDirectory(result.Forest);
        Assert.Equal(
            [
                "Enter the annex through the side door.",
                "Go down to the basement.",
                "Follow the corridor to the boiler room.",
                "Look behind the boiler.",
                "Open the recessed panel.",
                "Check the small alcove.",
            ],
            directory.GetDirections("Stopcock Key"));
    }

    [Fact]
    public void ItemDirectlyUnderARoot_HasASingleDirection()
    {
        var result = _parser.ParseFile(FixturePath("Data-complex.txt"));

        Assert.True(result.Success);
        var directory = new ItemDirectory(result.Forest);
        Assert.Equal(["Enter the warehouse."], directory.GetDirections("Forklift Key"));
    }

    [Fact]
    public void ItemAfterADeepBranch_BacktracksToItsOwnLevel()
    {
        var result = _parser.ParseFile(FixturePath("Data-complex.txt"));

        Assert.True(result.Success);
        var directory = new ItemDirectory(result.Forest);
        Assert.Equal(
            ["Enter the warehouse.", "Walk to aisle three."],
            directory.GetDirections("Pallet Jack Handle"));
    }

    private static string FixturePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "Fixtures", fileName);
}
