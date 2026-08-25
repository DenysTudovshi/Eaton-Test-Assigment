using ItemFinder.Application;
using ItemFinder.ConsoleApp;

namespace ItemFinder.ConsoleApp.Tests;

public class ItemFinderAppTests
{
    private static readonly string[] ListBlock =
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
    ];

    private static readonly string[] MobilePhoneDirections =
    [
        "",
        "Walk to the end of the hall.",
        "Turn right.",
        "Go through the door at the end of the hall.",
        "Look on top of the desk.",
        "",
    ];

    private const string ContinuePrompt = "Press Enter to continue...";

    private static ItemFinderApp CreateApp(FakeConsole console) =>
        new(console, StubParser.WithSampleForest(), "Data.txt");

    [Fact]
    public void Run_RendersTheAlphabeticalListPerContract()
    {
        var console = new FakeConsole();

        CreateApp(console).Run();

        Assert.Equal(ListBlock, console.Output.Take(ListBlock.Length));
    }

    [Fact]
    public void Run_SelectingItemFour_PrintsDirectionsThenWaitsForEnterBeforeRelisting()
    {
        var console = new FakeConsole("4", "");

        var exitCode = CreateApp(console).Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(
            [.. ListBlock, .. MobilePhoneDirections, ContinuePrompt, .. ListBlock],
            console.Output);
    }

    [Fact]
    public void Run_InputExhaustedAtContinuePrompt_ExitsZeroWithoutRelisting()
    {
        var console = new FakeConsole("4");

        var exitCode = CreateApp(console).Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(
            [.. ListBlock, .. MobilePhoneDirections, ContinuePrompt],
            console.Output);
    }

    [Fact]
    public void Run_InputExhausted_ExitsZeroWithoutLooping()
    {
        var console = new FakeConsole();

        var exitCode = CreateApp(console).Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(ListBlock.Length, console.Output.Count);
    }

    [Theory]
    [InlineData("q")]
    [InlineData("Q")]
    [InlineData(" q ")]
    public void Run_QuitCommand_ExitsZero(string quit)
    {
        var console = new FakeConsole(quit);

        var exitCode = CreateApp(console).Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(ListBlock.Length, console.Output.Count);
    }

    [Fact]
    public void Run_MultipleSelections_WorkInOneSession()
    {
        var console = new FakeConsole("4", "", "2", "", "q");

        var exitCode = CreateApp(console).Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(2, console.Output.Count(line => line == "Walk to the end of the hall."));
        Assert.Contains("Open the cabinet on the left.", console.Output);
    }

    [Fact]
    public void Run_AnyInputAtContinuePrompt_ShowsTheListAgain()
    {
        var console = new FakeConsole("4", "anything", "q");

        var exitCode = CreateApp(console).Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(
            [.. ListBlock, .. MobilePhoneDirections, ContinuePrompt, .. ListBlock],
            console.Output);
    }

    [Theory]
    [InlineData("abc")]
    [InlineData("0")]
    [InlineData("99")]
    [InlineData("")]
    public void Run_InvalidInput_PrintsOneHintAndKeepsAccepting(string bad)
    {
        var console = new FakeConsole(bad, "2", "", "q");

        var exitCode = CreateApp(console).Run();

        Assert.Equal(0, exitCode);
        Assert.Equal(1, console.Output.Count(line =>
            line == "Please enter a number between 1 and 5, or 'q' to quit."));
        Assert.Contains("Open the cabinet on the left.", console.Output);
    }

    [Fact]
    public void Run_ParseFailure_PrintsEachErrorAndExitsOne()
    {
        var failure = ParseResult.Failed(new ParseError(ParseErrorKind.FileNotFound, "The data file 'Data.txt' could not be found."));
        var console = new FakeConsole();
        var app = new ItemFinderApp(console, new StubParser(failure), "Data.txt");

        var exitCode = app.Run();

        Assert.Equal(1, exitCode);
        Assert.Contains("The data file 'Data.txt' could not be found.", console.Output);
    }
}