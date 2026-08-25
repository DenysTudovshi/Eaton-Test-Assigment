using ItemFinder.Application.Enums;

namespace ItemFinder.Application.Results;

/// <summary>A single problem found while parsing a data file.</summary>
/// <param name="Kind">Machine-readable category, for callers that branch on the error rather than display it.</param>
/// <param name="Message">Human-friendly description of what is wrong.</param>
/// <param name="LineNumber">1-based line number, when the problem is tied to a line.</param>
public sealed record ParseError(ParseErrorKind Kind, string Message, int? LineNumber = null);