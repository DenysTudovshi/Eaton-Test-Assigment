using ItemFinder.Application;
using ItemFinder.Domain;

namespace ItemFinder.Application.Tests;

public class ItemDirectoryLoaderTests
{
    private sealed class StubParser(ParseResult result) : IDataFileParser
    {
        public ParseResult ParseFile(string path) => result;

        public ParseResult ParseText(string text) => result;
    }

    [Fact]
    public void Load_ParsableFile_BuildsTheDirectory()
    {
        var shelf = new DirectionNode("Look at the shelf.");
        shelf.AddChild(new ItemNode("Book"));
        var loader = new ItemDirectoryLoader(new StubParser(ParseResult.Ok(new DirectionForest([shelf]))));

        var result = loader.Load("Data.txt");

        Assert.True(result.Success);
        var item = Assert.Single(result.Directory!.Items);
        Assert.Equal("Book", item.Name);
    }

    [Fact]
    public void Load_FailedParse_CarriesTheErrorsThrough()
    {
        var errors = new[]
        {
            new ParseError(ParseErrorKind.BlankLine, "Line 2 is blank; the data file must not contain blank lines.", 2),
            new ParseError(ParseErrorKind.DuplicateItem, "Line 5 repeats the item 'Cookies'; item names must be unique.", 5),
        };
        var loader = new ItemDirectoryLoader(new StubParser(ParseResult.Failed(errors)));

        var result = loader.Load("Data.txt");

        Assert.False(result.Success);
        Assert.Null(result.Directory);
        Assert.Equal(errors, result.Errors);
    }
}