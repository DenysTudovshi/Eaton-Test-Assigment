namespace ItemFinder.ConsoleApp.Configuration;

/// <summary>Resolves the data file to use: CLI argument first, then environment variable, then the bundled default.</summary>
public static class DataFilePathResolver
{
    public const string EnvironmentVariable = "ITEMFINDER_DATA_FILE";

    private const string DefaultFileName = "Data.txt";

    public static string Resolve(
        IReadOnlyList<string> args,
        Func<string, string?> getEnvironmentVariable,
        string baseDirectory)
    {
        ArgumentNullException.ThrowIfNull(args);
        ArgumentNullException.ThrowIfNull(getEnvironmentVariable);

        if (args.Count > 0 && !string.IsNullOrWhiteSpace(args[0]))
        {
            return args[0];
        }

        var fromEnvironment = getEnvironmentVariable(EnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(fromEnvironment))
        {
            return fromEnvironment;
        }

        return Path.Combine(baseDirectory, DefaultFileName);
    }
}