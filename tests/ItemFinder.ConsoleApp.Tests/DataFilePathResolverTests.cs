using ItemFinder.ConsoleApp.Configuration;

namespace ItemFinder.ConsoleApp.Tests;

public class DataFilePathResolverTests
{
    private const string BaseDirectory = @"C:\app";

    private static string? NoEnvironment(string name) => null;

    [Fact]
    public void Resolve_ArgumentProvided_WinsOverEverything()
    {
        var path = DataFilePathResolver.Resolve(
            ["custom\\My.txt"],
            name => "env\\Other.txt",
            BaseDirectory);

        Assert.Equal("custom\\My.txt", path);
    }

    [Fact]
    public void Resolve_NoArgument_UsesTheEnvironmentVariable()
    {
        var path = DataFilePathResolver.Resolve(
            [],
            name => name == DataFilePathResolver.EnvironmentVariable ? "env\\Other.txt" : null,
            BaseDirectory);

        Assert.Equal("env\\Other.txt", path);
    }

    [Fact]
    public void Resolve_NothingProvided_FallsBackToTheBundledDefault()
    {
        var path = DataFilePathResolver.Resolve([], NoEnvironment, BaseDirectory);

        Assert.Equal(Path.Combine(BaseDirectory, "Data.txt"), path);
    }

    [Fact]
    public void Resolve_BlankArgumentAndBlankVariable_AreTreatedAsAbsent()
    {
        var path = DataFilePathResolver.Resolve(["   "], name => "", BaseDirectory);

        Assert.Equal(Path.Combine(BaseDirectory, "Data.txt"), path);
    }
}
