namespace ItemFinder.ConsoleApp;

/// <summary>Console boundary, abstracted so the interaction flow is testable.</summary>
public interface IConsole
{
    void WriteLine(string text = "");

    /// <summary>Reads one input line; null when input is exhausted.</summary>
    string? ReadLine();
}
