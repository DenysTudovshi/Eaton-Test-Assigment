using ItemFinder.Application;
using ItemFinder.Domain;

namespace ItemFinder.ConsoleApp;

/// <summary>The interactive flow: parse the data file, list items, resolve a selection to directions.</summary>
public sealed class ItemFinderApp(IConsole console, IDataFileParser parser, string dataFilePath)
{
    public int Run()
    {
        try
        {
            return RunCore();
        }
        catch (Exception)
        {
            console.WriteLine("Something went wrong and the application had to stop. Please check the data file and try again.");
            return 1;
        }
    }

    private int RunCore()
    {
        var result = parser.ParseFile(dataFilePath);
        if (!result.Success)
        {
            foreach (var error in result.Errors)
            {
                console.WriteLine(error.Message);
            }

            return 1;
        }

        var directory = new ItemDirectory(result.Forest);

        while (true)
        {
            RenderItemList(directory);

            while (true)
            {
                var input = console.ReadLine();
                if (input is null)
                {
                    return 0; // input exhausted (for example, piped stdin)
                }

                var trimmed = input.Trim();
                if (trimmed.Equals("q", StringComparison.OrdinalIgnoreCase))
                {
                    return 0;
                }

                if (int.TryParse(trimmed, out var selection)
                    && selection >= 1
                    && selection <= directory.Items.Count)
                {
                    PrintDirections(directory.Items[selection - 1]);
                    console.WriteLine("Press Enter to continue...");
                    if (console.ReadLine() is null)
                    {
                        return 0; // input exhausted (for example, piped stdin)
                    }

                    break; // back to the item list
                }

                console.WriteLine(
                    $"Please enter a number between 1 and {directory.Items.Count}, or 'q' to quit.");
            }
        }
    }

    private void PrintDirections(LocatedItem item)
    {
        console.WriteLine();
        foreach (var step in item.Directions)
        {
            console.WriteLine(step);
        }

        console.WriteLine();
    }

    private void RenderItemList(ItemDirectory directory)
    {
        console.WriteLine("Available items:");
        console.WriteLine();
        for (var i = 0; i < directory.Items.Count; i++)
        {
            console.WriteLine($"[{i + 1}] - {directory.Items[i].Name}");
        }

        console.WriteLine();
        console.WriteLine("What item would you like to search for?");
    }
}