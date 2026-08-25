using System.Diagnostics.CodeAnalysis;

using ItemFinder.Application;
using ItemFinder.ConsoleApp.Input;
using ItemFinder.ConsoleApp.Views;

namespace ItemFinder.ConsoleApp;

/// <summary>The interactive session: load the data file, then loop list → select → directions → pause.</summary>
public sealed class ItemFinderApp(
    ItemDirectoryLoader loader,
    ItemListView itemList,
    DirectionsView directions,
    ErrorView errors,
    SelectionReader selection,
    string dataFilePath)
{
    [SuppressMessage("Design", "CA1031:Do not catch general exception types",
        Justification = "Last-resort boundary: whatever fails, the user gets one friendly line and exit code 1, never a stack trace.")]
    public int Run()
    {
        try
        {
            return RunCore();
        }
        catch (Exception)
        {
            errors.RenderUnexpectedFailure();
            return 1;
        }
    }

    private int RunCore()
    {
        var load = loader.Load(dataFilePath);
        if (!load.Success)
        {
            errors.Render(load.Errors);
            return 1;
        }

        var directory = load.Directory;

        while (true)
        {
            itemList.Render(directory);

            var index = selection.ReadSelection(directory.Items.Count);
            if (index is null)
            {
                return 0;
            }

            directions.Render(directory.Items[index.Value]);

            if (!selection.WaitForEnter())
            {
                return 0;
            }
        }
    }
}
