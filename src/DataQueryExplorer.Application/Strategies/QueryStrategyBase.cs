namespace DataQueryExplorer.Application.Strategies;

/// <summary>
/// Abstract base class for all query strategies.
/// Provides shared helpers for fetching data, writing to storage, and managing repositories.
/// Subclasses implement <see cref="ExecuteAsync"/> with their specific join/traversal logic.
/// </summary>
public abstract class QueryStrategyBase : IQueryStrategy
{
    protected readonly IDatabaseClient DatabaseClient;
    protected readonly IApplicationLogger Logger;
    protected readonly SqlQueryParser QueryParser;

    protected IDatabaseRepository<JObject>? ParentRepository;
    protected IDatabaseRepository<JObject>? SecondRepository;
    protected IDatabaseRepository<JObject>? ThirdRepository;

    protected QueryStrategyBase(
        IDatabaseClient databaseClient,
        IApplicationLogger logger,
        SqlQueryParser queryParser)
    {
        DatabaseClient = databaseClient;
        Logger = logger;
        QueryParser = queryParser;
    }

    public abstract Task ExecuteAsync(StrategyExecutionContext context);

    // -----------------------------------------------------------------------
    // Cosmos fetch helpers
    // -----------------------------------------------------------------------

    /// <summary>Fetches one page of results using a continuation token.</summary>
    protected async Task<(IEnumerable<JObject> Items, string? Token)> FetchPagedAsync(
        IDatabaseRepository<JObject> repo,
        string query,
        string? continuationToken,
        IDictionary<string, object>? parameters = null,
        int maxItems = AppConstants.DefaultMaxItemsPerPage)
    {
        PagedResult<JObject> result = await repo.QueryPagedAsync(query, continuationToken, maxItems, parameters);
        using (result)
        {
            if (result.Items.Any())
                return (result.Items, result.ContinuationToken);

            Logger.LogInfo($"No results found for query: {query}");
            return (System.Linq.Enumerable.Empty<JObject>(), null);
        }
    }

    /// <summary>Fetches all results for a query (no paging).</summary>
    protected async Task<IEnumerable<JObject>> FetchAllAsync(
        IDatabaseRepository<JObject> repo,
        string query,
        IDictionary<string, object>? parameters = null)
    {
        IEnumerable<JObject> items = await repo.QueryAsync(query, parameters);
        IReadOnlyList<JObject> list = items.ToList();
        if (!list.Any())
            Logger.LogInfo($"No results for query: {query}");
        return list;
    }

    /// <summary>
    /// Executes a COUNT query against the repository and returns the result.
    /// </summary>
    protected async Task<int> GetCountAsync(IDatabaseRepository<JObject> repo, string query)
    {
        string countQuery = QueryParser.BuildCountQuery(query);
        IEnumerable<JObject> results = await repo.QueryAsync(countQuery);
        return results.FirstOrDefault()?.Value<int>(AppConstants.CountColumnName) ?? 0;
    }

    // -----------------------------------------------------------------------
    // Parameter building helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Builds a parameter dictionary (@name → value) from a parent JObject result
    /// using the given parameter property names.
    /// </summary>
    protected static IDictionary<string, object> BuildParameters(
        IEnumerable<string> paramNames,
        JObject source)
    {
        return paramNames.ToDictionary(
            p => "@" + p,
            p => (object)(source.Value<string>(p) ?? string.Empty));
    }

    /// <summary>
    /// Builds a parameter dictionary from a pre-read Excel input row (header→value map).
    /// </summary>
    protected static IDictionary<string, object> BuildParameters(
        IEnumerable<string> paramNames,
        IReadOnlyDictionary<string, string> row)
    {
        return paramNames.ToDictionary(
            p => "@" + p,
            p => (object)(row.GetValueOrDefault(p) ?? string.Empty));
    }

    // -----------------------------------------------------------------------
    // Excel write helpers
    // -----------------------------------------------------------------------

    /// <summary>
    /// Appends one Cosmos document's values (for the given header columns) to the writer.
    /// Returns the 1-based row number where data was written so callers can add extra cells later.
    /// </summary>
    protected static int WriteDocument(
        IStorageWriter writer,
        string[] headers,
        JObject document)
    {
        IEnumerable<string?> values = headers.Select(h => document.Value<string>(h));
        return writer.WriteRow(values);
    }

    // -----------------------------------------------------------------------
    // IDisposable
    // -----------------------------------------------------------------------

    public void Dispose()
    {
        ParentRepository?.Dispose();
        SecondRepository?.Dispose();
        ThirdRepository?.Dispose();
        GC.SuppressFinalize(this);
    }
}
