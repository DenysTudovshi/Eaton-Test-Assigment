using ItemFinder.Application;
using ItemFinder.ConsoleApp;

namespace ItemFinder.ConsoleApp.Tests;

public class ItemFinderAppTests
{
    [Fact]
    public void Run_RendersTheAlphabeticalListPerContract()
    {
        var console = new FakeConsole();
        var app = new ItemFinderApp(console, StubParser.WithSampleForest(), "Data.txt");

        app.Run();

        Assert.Equal(
            [
                "Available items:",
                "",
                "[1] - Coffee Mug",
                "[2] - Cookies",
                "[3] - Milk",
                "[4] - Mobile Phone",
                "[5] - Pencils",
                "",
                "What item would you like to search for?",
            ],
            console.Output.Take(9));
    }

    [Fact]
    public void Run_SelectingItemFour_PrintsTheWorkedExampleDirections()
    {
        var console = new FakeConsole("4");
        var app = new ItemFinderApp(console, StubParser.WithSampleForest(), "Data.txt");

        var exitCode = app.Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(
            [
                "",
                "Walk to the end of the hall.",
                "Turn right.",
                "Go through the door at the end of the hall.",
                "Look on top of the desk.",
            ],
            console.Output.Skip(9).Take(5));
    }

    [Fact]
    public void Run_ParseFailure_PrintsEachErrorAndExitsOne()
    {
        var failure = ParseResult.Failed(new ParseError("The data file 'Data.txt' could not be found."));
        var console = new FakeConsole();
        var app = new ItemFinderApp(console, new StubParser(failure), "Data.txt");

        var exitCode = app.Run();

        Assert.Equal(1, exitCode);
        Assert.Contains("The data file 'Data.txt' could not be found.", console.Output);
    }
}
