using ItemFinder.ConsoleApp.IO;
using ItemFinder.Domain.Entities;
using ItemFinder.Domain.ValueObjects;

namespace ItemFinder.ConsoleApp.Views;

/// <summary>Renders an item's direction steps, one per line, framed by blank lines.</summary>
public sealed class DirectionsView(IConsole console)
{
    public void Render(LocatedItem item)
    {
        ArgumentNullException.ThrowIfNull(item);

        console.WriteLine();
        foreach (var step in item.Directions)
        {
            console.WriteLine(step);
        }

        console.WriteLine();
    }
}