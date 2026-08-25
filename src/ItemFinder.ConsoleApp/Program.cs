using ItemFinder.Application;
using ItemFinder.ConsoleApp;
using ItemFinder.Infrastructure;

using Microsoft.Extensions.DependencyInjection;

using var services = new ServiceCollection()
    .AddSingleton<IConsole, SystemConsole>()
    .AddSingleton<IDataFileParser, DataFileParser>()
    .AddSingleton<ItemDirectoryLoader>()
    .BuildServiceProvider();

var dataFilePath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "Data.txt");

var app = new ItemFinderApp(
    services.GetRequiredService<IConsole>(),
    services.GetRequiredService<ItemDirectoryLoader>(),
    dataFilePath);

return app.Run();