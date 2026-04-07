namespace DataQueryExplorer.Infrastructure.CosmosDb;

/// <summary>
/// Generic Cosmos DB repository. Created per-container via <see cref="CosmosDbClient.GetRepository{T}"/>.
/// </summary>
public sealed class CosmosDbRepository<T> : IDatabaseRepository<T> where T : class
{
    private readonly Container _container;
    private readonly IApplicationLogger _logger;

    internal CosmosDbRepository(Container container, IApplicationLogger logger)
    {
        _container = container;
        _logger = logger;
    }

    /// <summary>
    /// Executes the query and returns all matching results (no continuation token).
    /// </summary>
    public async Task<IEnumerable<T>> QueryAsync(
        string query,
        IDictionary<string, object>? parameters = null)
    {
        QueryDefinition queryDef = BuildQueryDefinition(query, parameters);
        FeedIterator<T> iterator = _container.GetItemQueryIterator<T>(queryDef);
        List<T> results = new();
        while (iterator.HasMoreResults)
        {
            FeedResponse<T> response = await iterator.ReadNextAsync();
            results.AddRange(response);
        }
        return results;
    }

    /// <summary>
    /// Executes the query with paging support using a continuation token.
    /// Retrieves up to <paramref name="maxItemCount"/> items starting from the given token.
    /// </summary>
    public async Task<PagedResult<T>> QueryPagedAsync(
        string query,
        string? continuationToken,
        int maxItemCount,
        IDictionary<string, object>? parameters = null)
    {
        QueryDefinition queryDef = BuildQueryDefinition(query, parameters);
        List<T> results = new();
        int remaining = Math.Max(1, maxItemCount);
        string? nextToken = continuationToken;
        bool keepFetching = true;

        while (keepFetching)
        {
            QueryRequestOptions options = new() { MaxItemCount = remaining };
            FeedIterator<T> iterator = _container.GetItemQueryIterator<T>(queryDef, nextToken, options);

            if (iterator.HasMoreResults)
            {
                FeedResponse<T> response = await iterator.ReadNextAsync();
                results.AddRange(response);
                nextToken = response.ContinuationToken;
            }
            else
            {
                nextToken = null;
            }

            if (string.IsNullOrWhiteSpace(nextToken))
            {
                keepFetching = false;
            }
            else if (results.Count < remaining)
            {
                remaining -= results.Count;
            }
            else
            {
                keepFetching = false;
            }
        }

        return new PagedResult<T>(results, nextToken);
    }

    private static QueryDefinition BuildQueryDefinition(string query, IDictionary<string, object>? parameters)
    {
        QueryDefinition queryDef = new(query);
        if (parameters is not null)
            foreach ((string key, object value) in parameters)
                queryDef.WithParameter(key, value);
        return queryDef;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
