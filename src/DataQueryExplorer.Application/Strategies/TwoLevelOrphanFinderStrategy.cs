namespace DataQueryExplorer.Application.Strategies;

/// <summary>
/// Two-container orphan finder.
/// Writes only parent documents for which NO matching child document was found.
/// Useful for detecting dangling / orphaned records.
/// </summary>
public sealed class TwoLevelOrphanFinderStrategy : QueryStrategyBase
{
    public TwoLevelOrphanFinderStrategy(
        IDatabaseClient databaseClient,
        IApplicationLogger logger,
        SqlQueryParser queryParser)
        : base(databaseClient, logger, queryParser) { }

    public override async Task ExecuteAsync(StrategyExecutionContext context)
    {
        QueryExecutionRequest req = context.Request;
        ParentRepository = DatabaseClient.GetRepository<JObject>(req.ParentContainerName, context.DatabaseName);
        SecondRepository = DatabaseClient.GetRepository<JObject>(req.SecondContainerName!, context.DatabaseName);

        string[] parentHeaders = QueryParser.ExtractColumnHeaders(req.ParentQuery);
        IStorageWriter parentWriter = context.StorageFactory.CreateWriter("OrphanedParentResult");
        parentWriter.WriteHeaders(parentHeaders);

        Logger.LogToConsole($"Getting count from '{req.ParentContainerName}'...");
        int total = await GetCountAsync(ParentRepository, req.ParentQuery);
        Logger.LogToConsole($"Found {total} parent record(s). Checking for orphans...");

        IReadOnlyList<string> childParamKeys = QueryParser.ExtractParameters(req.SecondQuery!);
        string? token = null;
        IEnumerable<JObject> parentResults;

        IProgressReporter progress = context.ProgressFactory.Create(Math.Max(total, 1));
        using (progress)
        {
            do
            {
                (parentResults, token) = await FetchPagedAsync(ParentRepository, req.ParentQuery, token);
                foreach (JObject parentDoc in parentResults)
                {
                    IDictionary<string, object> childParams = BuildParameters(childParamKeys, parentDoc);
                    IEnumerable<JObject> childResults = await FetchAllAsync(SecondRepository, req.SecondQuery!, childParams);

                    if (!childResults.Any())
                        WriteDocument(parentWriter, parentHeaders, parentDoc);

                    progress.Tick();
                }
            } while (parentResults.Any() && token is not null);
        }
    }
}
