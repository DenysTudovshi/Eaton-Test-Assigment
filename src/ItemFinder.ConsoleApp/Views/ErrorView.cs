using ItemFinder.Application.Enums;
using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Results;
using ItemFinder.Application.Services;
using ItemFinder.ConsoleApp.IO;

namespace ItemFinder.ConsoleApp.Views;

/// <summary>Renders failures as friendly lines; never a stack trace.</summary>
public sealed class ErrorView(IConsole console)
{
    public void Render(IReadOnlyList<ParseError> errors)
    {
        ArgumentNullException.ThrowIfNull(errors);

        foreach (var error in errors)
        {
            console.WriteLine(error.Message);
        }
    }

    public void RenderUnexpectedFailure() =>
        console.WriteLine("Something went wrong and the application had to stop. Please check the data file and try again.");
}