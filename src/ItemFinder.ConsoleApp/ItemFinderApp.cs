using ItemFinder.Application;

namespace ItemFinder.ConsoleApp;

/// <summary>The interactive flow: parse the data file, list items, resolve a selection to directions.</summary>
public sealed class ItemFinderApp(IConsole console, IDataFileParser parser, string dataFilePath)
{
    public int Run()
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

        var directory = new ItemDirectory(result.Forest!);
        RenderItemList(directory);

        var input = console.ReadLine();
        if (input is not null
            && int.TryParse(input.Trim(), out var selection)
            && selection >= 1
            && selection <= directory.AvailableItems.Count)
        {
            var itemName = directory.AvailableItems[selection - 1];
            console.WriteLine();
            foreach (var step in directory.GetDirections(itemName)!)
            {
                console.WriteLine(step);
            }
        }

        return 0;
    }

    private void RenderItemList(ItemDirectory directory)
    {
        console.WriteLine("Available items:");
        console.WriteLine();
        for (var i = 0; i < directory.AvailableItems.Count; i++)
        {
            console.WriteLine($"[{i + 1}] - {directory.AvailableItems[i]}");
        }

        console.WriteLine();
        console.WriteLine("What item would you like to search for?");
    }
}
