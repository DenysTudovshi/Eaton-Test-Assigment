using ItemFinder.Application;
using ItemFinder.Domain;

namespace ItemFinder.ConsoleApp;

/// <summary>The interactive flow: parse the data file, list items, resolve a selection to directions.</summary>
public sealed class ItemFinderApp(IConsole console, ItemDirectoryLoader loader, string dataFilePath)
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
        var load = loader.Load(dataFilePath);
        if (!load.Success)
        {
            foreach (var error in load.Errors)
            {
                console.WriteLine(error.Message);
            }

            return 1;
        }

        var directory = load.Directory;

        while (true)
        {
            RenderItemList(directory);

            var index = ReadSelection(directory.Items.Count);
            if (index is null)
            {
                return 0;
            }

            PrintDirections(directory.Items[index.Value]);

            console.WriteLine("Press Enter to continue...");
            if (console.ReadLine() is null)
            {
                return 0; // input exhausted (for example, piped stdin)
            }
        }
    }

    /// <summary>Reads until a valid item number arrives; null means quit ('q' or end of input).</summary>
    private int? ReadSelection(int itemCount)
    {
        while (true)
        {
            var input = console.ReadLine();
            if (input is null)
            {
                return null;
            }

            var trimmed = input.Trim();
            if (trimmed.Equals("q", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            if (int.TryParse(trimmed, out var selection) && selection >= 1 && selection <= itemCount)
            {
                return selection - 1;
            }

            console.WriteLine($"Please enter a number between 1 and {itemCount}, or 'q' to quit.");
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