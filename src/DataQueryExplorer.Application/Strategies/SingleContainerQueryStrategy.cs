namespace DataQueryExplorer.Application.Strategies;

/// <summary>
/// Queries a single container.
/// • Without @params  → streams results page-by-page using a continuation token.
/// • With @params     → executes one query per row in the pre-read input Excel data.
/// </summary>
public sealed class SingleContainerQueryStrategy : QueryStrategyBase
{
    public SingleContainerQueryStrategy(
        IDatabaseClient databaseClient,
        IApplicationLogger logger,
        SqlQueryParser queryParser)
        : base(databaseClient, logger, queryParser) { }

    public override async Task ExecuteAsync(StrategyExecutionContext context)
    {
        QueryExecutionRequest req = context.Request;
        ParentRepository = DatabaseClient.GetRepository<JObject>(req.ParentContainerName, context.DatabaseName);

        string[] headers = QueryParser.ExtractColumnHeaders(req.ParentQuery);
        IStorageWriter writer = context.StorageFactory.CreateWriter(req.ParentContainerName);
        writer.WriteHeaders(headers);

        if (req.ParameterRows is { Count: > 0 })
        {
            await ExecuteWithParameterRowsAsync(req, headers, writer, context.ProgressFactory);
        }
        else
        {
            await ExecutePagedAsync(req, headers, writer, context.ProgressFactory);
        }
    }

    private async Task ExecuteWithParameterRowsAsync(
        QueryExecutionRequest req,
        string[] headers,
        IStorageWriter writer,
        IProgressReporterFactory progressFactory)
    {
        IReadOnlyList<string> paramKeys = QueryParser.ExtractParameters(req.ParentQuery);
        Logger.LogToConsole($"Executing parameterised query over {req.ParameterRows!.Count} input rows...");

        IProgressReporter progress = progressFactory.Create(req.ParameterRows.Count);
        using (progress)
        {
            foreach (IReadOnlyDictionary<string, string> row in req.ParameterRows)
            {
                IDictionary<string, object> parameters = BuildParameters(paramKeys, row);
                IEnumerable<JObject> results = await FetchAllAsync(ParentRepository!, req.ParentQuery, parameters);
                foreach (JObject doc in results)
                    WriteDocument(writer, headers, doc);
                progress.Tick();
            }
        }
    }

    private async Task ExecutePagedAsync(
        QueryExecutionRequest req,
        string[] headers,
        IStorageWriter writer,
        IProgressReporterFactory progressFactory)
    {
        Logger.LogToConsole($"Getting count from container '{req.ParentContainerName}'...");
        int total = await GetCountAsync(ParentRepository!, req.ParentQuery);
        Logger.LogToConsole($"Found {total} record(s). Fetching now...");

        IProgressReporter progress = progressFactory.Create(Math.Max(total, 1));
        using (progress)
        {
            string? token = null;
            IEnumerable<JObject> results;
            do
            {
                GC.Collect();
                (results, token) = await FetchPagedAsync(ParentRepository!, req.ParentQuery, token);
                foreach (JObject doc in results)
                {
                    WriteDocument(writer, headers, doc);
                    progress.Tick();
                }
            } while (results.Any() && token is not null);
        }
    }
}
