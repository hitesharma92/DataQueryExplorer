namespace DataQueryExplorer.Console;

/// <summary>
/// Top-level orchestrator. Coordinates user input collection, strategy selection,
/// execution, and output file saving.
/// </summary>
public sealed class AppRunner
{
    private readonly IDatabaseClient _dbClient;
    private readonly ConsoleInputCollector _inputCollector;
    private readonly ConsoleMenu _menu;
    private readonly QueryStrategyFactory _strategyFactory;
    private readonly IProgressReporterFactory _progressFactory;
    private readonly IApplicationLogger _logger;
    private readonly IServiceProvider _serviceProvider;

    public AppRunner(
        IDatabaseClient dbClient,
        ConsoleInputCollector inputCollector,
        ConsoleMenu menu,
        QueryStrategyFactory strategyFactory,
        IProgressReporterFactory progressFactory,
        IApplicationLogger logger,
        IServiceProvider serviceProvider)
    {
        _dbClient = dbClient;
        _inputCollector = inputCollector;
        _menu = menu;
        _strategyFactory = strategyFactory;
        _progressFactory = progressFactory;
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    public async Task RunAsync()
    {
        // 1. Connection
        string endpoint = _inputCollector.PromptEndpoint();
        string key = _inputCollector.PromptKey();
        await _dbClient.VerifyConnectionAsync(endpoint, key);

        // 2. Database selection
        string databaseName = await _inputCollector.SelectDatabaseAsync();
        _logger.LogToConsole($"Using database: {databaseName}");

        // 3. Output path
        string outputFolder = _inputCollector.PromptOutputPath();
        if (!Directory.Exists(outputFolder))
        {
            _logger.LogToConsole($"Output folder '{outputFolder}' does not exist. Creating it...");
            Directory.CreateDirectory(outputFolder);
        }

        // 4. Strategy selection
        QueryStrategyType strategyType = _menu.SelectStrategy();

        // 5. Collect query inputs
        QueryExecutionRequest request = await _inputCollector.CollectRequestAsync(strategyType, databaseName);

        // 6. Build output file path
        string outputFile = Path.Combine(outputFolder,
            $"{databaseName} - QueryOutput_{strategyType}_{DateTime.Now.ToFileTime()}.xlsx");

        // 7. Execute — both strategy and factory are Transient; the caller owns disposal
        IStorageWriterFactory storageFactory = _serviceProvider.GetRequiredService<IStorageWriterFactory>();
        IQueryStrategy strategy = _strategyFactory.Create(strategyType);

        using (strategy)
        using (storageFactory)
        {
            StrategyExecutionContext executionContext = new StrategyExecutionContext
            {
                Request = request,
                DatabaseName = databaseName,
                StorageFactory = storageFactory,
                ProgressFactory = _progressFactory
            };

            _logger.LogToConsole($"\nStarting execution: {strategyType}...");
            await strategy.ExecuteAsync(executionContext);

            // 8. Save Excel output — must happen before storageFactory is disposed
            _logger.LogToConsole($"\nSaving output to: {outputFile}");
            await storageFactory.SaveAsync(outputFile);
            _logger.LogToConsole("Done.");
        }
    }
}
