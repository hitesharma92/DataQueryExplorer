namespace DataQueryExplorer.Infrastructure.CosmosDb;

/// <summary>
/// Thread-safe Cosmos DB client wrapper that implements <see cref="IDatabaseClient"/>.
/// Register as Singleton in DI — the underlying CosmosClient SDK recommends a single instance.
/// </summary>
public sealed class CosmosDbClient : IDatabaseClient, IDisposable
{
    private CosmosClient? _cosmosClient;
    private readonly IApplicationLogger _logger;

    public CosmosDbClient(IApplicationLogger logger)
    {
        _logger = logger;
    }

    public async Task VerifyConnectionAsync(string endpoint, string key)
    {
        _logger.LogToConsole("Connecting to database...");
        try
        {
            _cosmosClient = new CosmosClientBuilder(endpoint, key)
                .WithConnectionModeGateway()
                .Build();

            // Verify connectivity by reading the account properties
            await _cosmosClient.ReadAccountAsync();
            _logger.LogToConsole("Database connection successful.");
        }
        catch (Exception ex)
        {
            _logger.LogError("Failed to connect to the database. Check your endpoint and key.", ex);
            throw;
        }
    }

    public async Task<IReadOnlyList<string>> ListDatabasesAsync()
    {
        EnsureClientInitialised();
        FeedIterator<DatabaseProperties> iterator = _cosmosClient!.GetDatabaseQueryIterator<DatabaseProperties>();
        List<string> databases = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<DatabaseProperties> response = await iterator.ReadNextAsync();
            foreach (DatabaseProperties db in response)
                databases.Add(db.Id);
        }
        databases.Sort(StringComparer.OrdinalIgnoreCase);
        return databases;
    }

    public async Task<IReadOnlyList<string>> ListContainersAsync(string databaseName)
    {
        EnsureClientInitialised();
        Database database = _cosmosClient!.GetDatabase(databaseName);
        FeedIterator<ContainerProperties> iterator = database.GetContainerQueryIterator<ContainerProperties>();
        List<string> containers = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<ContainerProperties> response = await iterator.ReadNextAsync();
            foreach (ContainerProperties container in response)
                containers.Add(container.Id);
        }
        containers.Sort(StringComparer.OrdinalIgnoreCase);
        return containers;
    }

    public IDatabaseRepository<T> GetRepository<T>(string containerName, string databaseName) where T : class
    {
        EnsureClientInitialised();
        Container container = _cosmosClient!.GetContainer(databaseName, containerName);
        return new CosmosDbRepository<T>(container, _logger);
    }

    private void EnsureClientInitialised()
    {
        if (_cosmosClient is null)
            throw new InvalidOperationException(
                "Database client is not initialised. Call VerifyConnectionAsync first.");
    }

    public void Dispose()
    {
        _cosmosClient?.Dispose();
        GC.SuppressFinalize(this);
    }
}
