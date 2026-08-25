using ItemFinder.Application.Enums;
using ItemFinder.Application.Interfaces;
using ItemFinder.Application.Results;
using ItemFinder.Application.Services;
using ItemFinder.ConsoleApp;
using ItemFinder.ConsoleApp.Input;
using ItemFinder.ConsoleApp.IO;
using ItemFinder.ConsoleApp.Views;

namespace ItemFinder.ConsoleApp.Tests;

/// <summary>Builds a fully wired app over a scripted console and a stubbed parser.</summary>
public static class TestApp
{
    public static ItemFinderApp Create(FakeConsole console, IDataFileParser parser, string dataFilePath = "Data.txt") =>
        new(
            new ItemDirectoryLoader(parser),
            new ItemListView(console),
            new DirectionsView(console),
            new ErrorView(console),
            new SelectionReader(console),
            dataFilePath);
}