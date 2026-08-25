using ItemFinder.Application;
using ItemFinder.Infrastructure;

namespace ItemFinder.Infrastructure.Tests;

public class DataFileParserStructureTests
{
    private readonly DataFileParser _parser = new();

    [Fact]
    public void ParseText_DepthJump_ReportsSkippedLevel()
    {
        const string text = "+ Enter the room.\n|  └──+ Open the box.";

        var result = _parser.ParseText(text);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Line 2 skips a level of the hierarchy.", error.Message);
        Assert.Equal(2, error.LineNumber);
    }

    [Fact]
    public void ParseText_EntryNestedUnderAnItem_ReportsFriendlyError()
    {
        const string text = "+ Enter the room.\n├── Item: Box\n|  └── Item: Marble";

        var result = _parser.ParseText(text);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Line 3 is nested under an item; items cannot contain anything beneath them.", error.Message);
        Assert.Equal(3, error.LineNumber);
    }

    [Fact]
    public void ParseText_DuplicateItemName_ReportsTheName()
    {
        const string text = """
            + Enter the room.
            ├──+ Check the shelf.
            |  └── Item: Cookies
            └──+ Check the drawer.
               └── Item: Cookies
            """;

        var result = _parser.ParseText(text);

        Assert.False(result.Success);
        var error = Assert.Single(result.Errors);
        Assert.Equal("Line 5 repeats the item 'Cookies'; item names must be unique.", error.Message);
        Assert.Equal(5, error.LineNumber);
    }

    [Fact]
    public void ParseText_DuplicateDetection_AppliesAfterTrimming()
    {
        const string text = "+ Enter.\n├──+ Look left.\n|  └── Item: Milk  \n└──+ Look right.\n   └── Item: Milk";

        var result = _parser.ParseText(text);

        Assert.False(result.Success);
        Assert.Contains("repeats the item 'Milk'", Assert.Single(result.Errors).Message);
    }

    [Fact]
    public void ParseText_CrlfInput_ParsesIdenticallyToLf()
    {
        const string lf = "+ Enter.\n└──+ Look.\n   └── Item: Box";
        var crlf = lf.Replace("\n", "\r\n");

        var fromLf = _parser.ParseText(lf);
        var fromCrlf = _parser.ParseText(crlf);

        Assert.True(fromCrlf.Success);
        Assert.Equal(
            new ItemDirectory(fromLf.Forest!).AvailableItems,
            new ItemDirectory(fromCrlf.Forest!).AvailableItems);
    }

    [Fact]
    public void ParseText_Utf8ByteOrderMark_IsTolerated()
    {
        const string text = "﻿+ Enter.\n└──+ Look.\n   └── Item: Box";

        var result = _parser.ParseText(text);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void ParseText_SingleTrailingNewline_IsTolerated()
    {
        const string text = "+ Enter.\n└──+ Look.\n   └── Item: Box\n";

        var result = _parser.ParseText(text);

        Assert.True(result.Success, string.Join("; ", result.Errors.Select(e => e.Message)));
    }

    [Fact]
    public void ParseText_DirectionWithoutItems_ParsesAndContributesNothing()
    {
        const string text = """
            + Enter.
            ├──+ Check the empty cupboard.
            └──+ Check the drawer.
               └── Item: Pencils
            """;

        var result = _parser.ParseText(text);

        Assert.True(result.Success);
        Assert.Equal(["Pencils"], new ItemDirectory(result.Forest!).AvailableItems);
    }
}
