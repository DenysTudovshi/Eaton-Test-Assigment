using ItemFinder.Application;
using ItemFinder.ConsoleApp;
using ItemFinder.ConsoleApp.Configuration;
using ItemFinder.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

using var services = new ServiceCollection()
    .AddSingleton<IConsole, SystemConsole>()
    .AddSingleton<IDataFileParser, DataFileParser>()
    .AddSingleton<ItemDirectoryLoader>()
    .BuildServiceProvider();

var dataFilePath = DataFilePathResolver.Resolve(
    args, Environment.GetEnvironmentVariable, AppContext.BaseDirectory);

var app = new ItemFinderApp(
    services.GetRequiredService<IConsole>(),
    services.GetRequiredService<ItemDirectoryLoader>(),
    dataFilePath);

return app.Run();