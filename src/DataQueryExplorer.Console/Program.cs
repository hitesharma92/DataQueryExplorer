using System.Reflection;
using DataQueryExplorer.Application.Factories;
using DataQueryExplorer.Application.Services;
using DataQueryExplorer.Application.Strategies;
using DataQueryExplorer.Console;
using DataQueryExplorer.Console.Logging;
using DataQueryExplorer.Console.UI;
using DataQueryExplorer.Domain.Interfaces;
using DataQueryExplorer.Infrastructure.CosmosDb;
using DataQueryExplorer.Infrastructure.Excel;
using Microsoft.Extensions.DependencyInjection;

// -----------------------------------------------------------------------
// Composition root — register all services
// -----------------------------------------------------------------------
ServiceCollection services = new();

// Singletons (shared across the entire app lifetime)
services.AddSingleton<IApplicationLogger, ConsoleApplicationLogger>();
services.AddSingleton<IDatabaseClient, CosmosDbClient>();
services.AddSingleton<SqlQueryParser>();
services.AddSingleton<InputExcelReader>();
services.AddSingleton<ConsoleMenu>();
services.AddSingleton<IProgressReporterFactory, ConsoleProgressReporterFactory>();
services.AddSingleton<ConsoleSelectorUI>();
services.AddSingleton<ConsoleInputCollector>();
services.AddSingleton<QueryStrategyFactory>();
services.AddSingleton<AppRunner>();

// Storage factory: Transient so each execution gets its own workbook
services.AddTransient<IStorageWriterFactory, ExcelStorageWriterFactory>();

// Strategies: Transient (hold disposable repository instances per-run)
services.AddTransient<SingleContainerQueryStrategy>();
services.AddTransient<TwoLevelJoinStrategy>();
services.AddTransient<TwoLevelOrphanFinderStrategy>();
services.AddTransient<TwoLevelDuplicateFinderStrategy>();
services.AddTransient<ThreeLevelJoinStrategy>();
services.AddTransient<ThreeLevelInnerJoinStrategy>();

ServiceProvider provider = services.BuildServiceProvider();
await using (provider)
{
    // -----------------------------------------------------------------------
    // Open log
    // -----------------------------------------------------------------------
    IApplicationLogger logger = provider.GetRequiredService<IApplicationLogger>();
    string? appName = Assembly.GetExecutingAssembly().GetName().Name ?? "DataQueryExplorer";
    logger.OpenLog(appName);
    logger.LogToConsole($"\n=== {appName} ===\n");

    // -----------------------------------------------------------------------
    // Run
    // -----------------------------------------------------------------------
    try
    {
        AppRunner runner = provider.GetRequiredService<AppRunner>();
        await runner.RunAsync();
    }
    catch (Exception ex)
    {
        logger.LogError("An unhandled error occurred.", ex);
    }
    finally
    {
        logger.LogToConsole("\nPress any key to exit...");
        System.Console.ReadKey();
        logger.CloseLog();
    }
}
