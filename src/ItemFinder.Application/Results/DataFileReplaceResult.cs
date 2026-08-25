namespace ItemFinder.Application.Results;

/// <summary>Outcome of replacing the managed data file: accepted with counts, or parse errors.</summary>
public sealed class DataFileReplaceResult
{
    private DataFileReplaceResult(bool createdNew, int itemCount, IReadOnlyList<ParseError> errors)
    {
        CreatedNew = createdNew;
        ItemCount = itemCount;
        Errors = errors;
    }

    /// <summary>True when the store held no file before this replace.</summary>
    public bool CreatedNew { get; }

    /// <summary>Number of items in the accepted file; 0 on failure.</summary>
    public int ItemCount { get; }

    public IReadOnlyList<ParseError> Errors { get; }

    public bool Success => Errors.Count == 0;

    public static DataFileReplaceResult Ok(bool createdNew, int itemCount) => new(createdNew, itemCount, []);

    public static DataFileReplaceResult Invalid(IReadOnlyList<ParseError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);
        return errors.Count == 0
            ? throw new ArgumentException("A rejected replace must carry at least one error.", nameof(errors))
            : new DataFileReplaceResult(createdNew: false, itemCount: 0, errors);
    }
}