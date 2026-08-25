using ItemFinder.Application;
using ItemFinder.ConsoleApp;
using ItemFinder.ConsoleApp.Configuration;
using ItemFinder.ConsoleApp.Input;
using ItemFinder.ConsoleApp.Views;
using ItemFinder.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

var dataFilePath = DataFilePathResolver.Resolve(
    args, Environment.GetEnvironmentVariable, AppContext.BaseDirectory);

using var services = new ServiceCollection()
    .AddSingleton<IConsole, SystemConsole>()
    .AddSingleton<IDataFileParser, DataFileParser>()
    .AddSingleton<ItemDirectoryLoader>()
    .AddSingleton<ItemListView>()
    .AddSingleton<DirectionsView>()
    .AddSingleton<ErrorView>()
    .AddSingleton<SelectionReader>()
    .AddSingleton(provider => new ItemFinderApp(
        provider.GetRequiredService<ItemDirectoryLoader>(),
        provider.GetRequiredService<ItemListView>(),
        provider.GetRequiredService<DirectionsView>(),
        provider.GetRequiredService<ErrorView>(),
        provider.GetRequiredService<SelectionReader>(),
        dataFilePath))
    .BuildServiceProvider();

return services.GetRequiredService<ItemFinderApp>().Run();
