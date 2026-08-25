using ItemFinder.ConsoleApp;

namespace ItemFinder.ConsoleApp.Tests;

/// <summary>Scripted console: feeds queued input lines and records everything written.</summary>
public sealed class FakeConsole(params string[] inputLines) : IConsole
{
    private readonly Queue<string> _inputs = new(inputLines);

    public List<string> Output { get; } = [];

    public void WriteLine(string text = "") => Output.Add(text);

    public string? ReadLine() => _inputs.TryDequeue(out var line) ? line : null;
}
