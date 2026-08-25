using ItemFinder.Application;

namespace ItemFinder.ConsoleApp.Views;

/// <summary>Renders the numbered, alphabetical item list with its selection prompt.</summary>
public sealed class ItemListView(IConsole console)
{
    public void Render(ItemDirectory directory)
    {
        ArgumentNullException.ThrowIfNull(directory);

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
