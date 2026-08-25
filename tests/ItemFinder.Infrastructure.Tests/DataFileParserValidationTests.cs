using ItemFinder.Application;
using ItemFinder.Infrastructure;

namespace ItemFinder.Infrastructure.Tests;

public class DataFileParserValidationTests
{
    private readonly DataFileParser _parser = new();

    [Fact]
    public void ParseFile_MissingFile_ReportsFriendlyError()
    {
        var missingPath = Path.Combine(AppContext.BaseDirectory, "no-such-file.txt");

        var result = _parser.ParseFile(missingPath);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal($"The data file '{missingPath}' could not be found.", error.Message);
        Assert.Equal(ParseErrorKind.FileNotFound, error.Kind);
        Assert.Null(error.LineNumber);
    }

    [Fact]
    public void ParseFile_PathIsADirectory_ReportsFriendlyError()
    {
        var directoryPath = AppContext.BaseDirectory;

        var result = _parser.ParseFile(directoryPath);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Contains("could not be", error.Message);
        Assert.Equal(ParseErrorKind.FileNotFound, error.Kind);
        Assert.DoesNotContain("Exception", error.Message);
    }

    [Fact]
    public void ParseText_EmptyText_ReportsEmptyFile()
    {
        var result = _parser.ParseText(string.Empty);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("The data file is empty.", error.Message);
        Assert.Equal(ParseErrorKind.EmptyFile, error.Kind);
    }

    [Fact]
    public void ParseText_BlankInteriorLine_ReportsLineNumber()
    {
        const string text = "+ Enter the room.\n\n└──+ Open the box.\n   └── Item: Marble";

        var result = _parser.ParseText(text);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Line 2 is blank; the data file must not contain blank lines.", error.Message);
        Assert.Equal(ParseErrorKind.BlankLine, error.Kind);
        Assert.Equal(2, error.LineNumber);
    }

    [Fact]
    public void ParseText_FirstLineIsNotARoot_ReportsFriendlyError()
    {
        const string text = "├──+ Turn left.\n|  └── Item: Cookies";

        var result = _parser.ParseText(text);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Line 1 must be a root direction starting with '+ '.", error.Message);
        Assert.Equal(ParseErrorKind.FirstLineNotRoot, error.Kind);
        Assert.Equal(1, error.LineNumber);
    }

    [Theory]
    [InlineData("+ Enter.\n???+ Turn left.", 2)]           // garbage instead of prefix/glyph
    [InlineData("+ Enter.\n├──Item: Cookies", 2)]          // item marker without its leading space
    [InlineData("+ Enter.\n├──* Turn left.", 2)]           // unknown tail marker
    [InlineData("+ Enter.\n├──+ Go.\n|  └── Stuff", 3)]    // tail is neither direction nor item
    public void ParseText_MalformedLine_ReportsLineNumber(string text, int badLine)
    {
        var result = _parser.ParseText(text);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal($"Line {badLine} is not a valid direction or item line.", error.Message);
        Assert.Equal(ParseErrorKind.MalformedLine, error.Kind);
        Assert.Equal(badLine, error.LineNumber);
    }

    [Fact]
    public void ParseText_ErrorMessages_NeverLeakExceptionJargon()
    {
        var result = _parser.ParseText("+ Enter.\n!!bad");

        var error = Assert.Single(result.Errors);
        Assert.DoesNotContain("Exception", error.Message);
        Assert.DoesNotContain("Stack", error.Message);
    }

    [Fact]
    public void ParseFile_EmptyPath_ReportsFriendlyError()
    {
        var result = _parser.ParseFile(string.Empty);

        Assert.False(result.Success);
        Assert.Contains("could not be", Assert.Single(result.Errors).Message);
    }
}