using ItemFinder.Application;
using ItemFinder.Domain;

namespace ItemFinder.Application.Tests;

public class ParseResultTests
{
    [Fact]
    public void Ok_CarriesForestAndNoErrors()
    {
        var forest = new DirectionForest([]);

        var result = ParseResult.Ok(forest);

        Assert.True(result.Success);
        Assert.Same(forest, result.Forest);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void Failed_CarriesErrorsAndNoForest()
    {
        var error = new ParseError("Line 3 is blank; the data file must not contain blank lines.", 3);

        var result = ParseResult.Failed(error);

        Assert.False(result.Success);
        Assert.Null(result.Forest);
        var single = Assert.Single(result.Errors);
        Assert.Equal(3, single.LineNumber);
    }

    [Fact]
    public void Failed_WithoutErrors_Throws()
    {
        Assert.Throws<ArgumentException>(() => ParseResult.Failed(Array.Empty<ParseError>()));
    }

    [Fact]
    public void ParseError_LineNumberIsOptional()
    {
        var error = new ParseError("The data file could not be found.");

        Assert.Null(error.LineNumber);
    }
}