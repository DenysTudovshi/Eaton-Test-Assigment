namespace ItemFinder.ConsoleApp.Input;

/// <summary>Reads and validates user input for the selection flow.</summary>
public sealed class SelectionReader(IConsole console)
{
    /// <summary>Reads until a valid item number arrives; null means quit ('q' or end of input).</summary>
    public int? ReadSelection(int itemCount)
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

    /// <summary>Prompts for Enter before continuing; false means input is exhausted.</summary>
    public bool WaitForEnter()
    {
        console.WriteLine("Press Enter to continue...");
        return console.ReadLine() is not null;
    }
}
