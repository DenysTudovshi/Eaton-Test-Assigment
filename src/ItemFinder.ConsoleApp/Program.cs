using ItemFinder.Application;
using ItemFinder.ConsoleApp;
using ItemFinder.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

using var services = new ServiceCollection()
    .AddSingleton<IConsole, SystemConsole>()
    .AddSingleton<IDataFileParser, DataFileParser>()
    .BuildServiceProvider();

var dataFilePath = args.Length > 0
    ? args[0]
    : Path.Combine(AppContext.BaseDirectory, "Data.txt");

var app = new ItemFinderApp(
    services.GetRequiredService<IConsole>(),
    services.GetRequiredService<IDataFileParser>(),
    dataFilePath);

return app.Run();
