namespace ItemFinder.Application;

/// <summary>Machine-readable category of a parse error, independent of the message wording.</summary>
public enum ParseErrorKind
{
    FileNotFound,
    FileUnreadable,
    EmptyFile,
    BlankLine,
    FirstLineNotRoot,
    MalformedLine,
    SkippedLevel,
    NestedUnderItem,
    DuplicateItem,
}
