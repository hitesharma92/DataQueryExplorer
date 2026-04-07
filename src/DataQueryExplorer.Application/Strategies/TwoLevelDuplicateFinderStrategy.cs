namespace DataQueryExplorer.Application.Strategies;

/// <summary>
/// Two-container duplicate-finder.
/// Fetches child documents per parent, groups them by a specified property, and writes
/// only those child entries whose occurrence count exceeds the supplied threshold.
/// A boolean <c>IsChildFound</c> column is appended to the parent sheet.
/// </summary>
public sealed class TwoLevelDuplicateFinderStrategy : QueryStrategyBase
{
    public TwoLevelDuplicateFinderStrategy(
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
        string[] childHeaders = QueryParser.ExtractColumnHeaders(req.SecondQuery!);

        IStorageWriter parentWriter = context.StorageFactory.CreateWriter("ParentResult");
        parentWriter.WriteHeaders(parentHeaders);

        IStorageWriter childWriter = context.StorageFactory.CreateWriter("DuplicateChildResult");
        childWriter.WriteHeaders(childHeaders);

        int isChildFoundColumn = parentHeaders.Length + 1;

        Logger.LogToConsole($"Getting count from '{req.ParentContainerName}'...");
        int total = await GetCountAsync(ParentRepository, req.ParentQuery);
        Logger.LogToConsole($"Found {total} parent record(s). Running duplicate detection...");

        IReadOnlyList<string> childParamKeys = QueryParser.ExtractParameters(req.SecondQuery!);
        string? token = null;
        IEnumerable<JObject> parentResults;

        IProgressReporter progress = context.ProgressFactory.Create(Math.Max(total, 1));
        using (progress)
        {
            do
            {
                GC.Collect();
                (parentResults, token) = await FetchPagedAsync(ParentRepository, req.ParentQuery, token);
                foreach (JObject parentDoc in parentResults)
                {
                    int parentRow = WriteDocument(parentWriter, parentHeaders, parentDoc);

                    IDictionary<string, object> childParams = BuildParameters(childParamKeys, parentDoc);
                    List<JObject> childResults = (await FetchAllAsync(SecondRepository, req.SecondQuery!, childParams)).ToList();

                    bool childFound = false;
                    Dictionary<string, int> duplicates = DuplicateDetector.GetDuplicatesAboveThreshold(
                        childResults, req.GroupByProperty!, req.GroupByPropertyThreshold);

                    foreach ((string key, int count) in duplicates)
                    {
                        Logger.LogInfo($"Duplicate found: {req.GroupByProperty} = '{key}', count = {count}");
                        IEnumerable<JObject> matchingChildren = childResults
                            .Where(d => d[req.GroupByProperty!]?.ToString() == key);

                        foreach (JObject childDoc in matchingChildren)
                        {
                            childFound = true;
                            WriteDocument(childWriter, childHeaders, childDoc);
                        }
                    }

                    parentWriter.WriteCell(childFound.ToString(), parentRow, isChildFoundColumn);
                    progress.Tick();
                }
            } while (parentResults.Any() && token is not null);
        }
    }
}
