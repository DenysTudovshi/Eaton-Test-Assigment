namespace ItemFinder.Application;

/// <summary>A single problem found while parsing a data file.</summary>
/// <param name="Message">Human-friendly description of what is wrong.</param>
/// <param name="LineNumber">1-based line number, when the problem is tied to a line.</param>
public sealed record ParseError(string Message, int? LineNumber = null);