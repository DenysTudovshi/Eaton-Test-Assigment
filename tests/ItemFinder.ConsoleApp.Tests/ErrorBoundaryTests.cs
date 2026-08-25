using ItemFinder.Application;
using ItemFinder.ConsoleApp;

namespace ItemFinder.ConsoleApp.Tests;

public class ErrorBoundaryTests
{
    private sealed class ThrowingParser : IDataFileParser
    {
        public ParseResult ParseFile(string path) => throw new InvalidOperationException("boom");

        public ParseResult ParseText(string text) => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void Run_UnexpectedException_PrintsOneFriendlyLineAndExitsOne()
    {
        var console = new FakeConsole();
        var app = new ItemFinderApp(console, new ThrowingParser(), "Data.txt");

        var exitCode = app.Run();

        Assert.Equal(1, exitCode);
        var line = Assert.Single(console.Output);
        Assert.Equal("Something went wrong and the application had to stop. Please check the data file and try again.", line);
    }

    [Fact]
    public void Run_UnexpectedException_NeverLeaksExceptionDetails()
    {
        var console = new FakeConsole();
        var app = new ItemFinderApp(console, new ThrowingParser(), "Data.txt");

        app.Run();

        Assert.DoesNotContain(console.Output, line => line.Contains("boom"));
        Assert.DoesNotContain(console.Output, line => line.Contains("Exception"));
        Assert.DoesNotContain(console.Output, line => line.Contains("   at "));
    }

    [Fact]
    public void Run_ParseFailureWithMultipleErrors_PrintsEachOnItsOwnLine()
    {
        var failure = ParseResult.Failed(
        [
            new ParseError("Line 2 is blank; the data file must not contain blank lines.", 2),
            new ParseError("Line 5 repeats the item 'Cookies'; item names must be unique.", 5),
        ]);
        var console = new FakeConsole();
        var app = new ItemFinderApp(console, new StubParser(failure), "Data.txt");

        var exitCode = app.Run();

        Assert.Equal(1, exitCode);
        Assert.Equal(2, console.Output.Count);
    }

    [Fact]
    public void Run_HappyPath_ExitsZero()
    {
        var console = new FakeConsole("q");
        var app = new ItemFinderApp(console, StubParser.WithSampleForest(), "Data.txt");

        Assert.Equal(0, app.Run());
    }
}