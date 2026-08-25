namespace ItemFinder.ConsoleApp.IO;

/// <summary>The real console; the only place that touches System.Console.</summary>
public sealed class SystemConsole : IConsole
{
    public void WriteLine(string text = "") => Console.WriteLine(text);

    public string? ReadLine() => Console.ReadLine();
}