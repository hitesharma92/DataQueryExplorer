namespace DataQueryExplorer.Application.Strategies;

/// <summary>
/// Three-container inner-join query.
/// Writes a parent document only when at least one complete Parent → Second → Third chain exists.
/// Produces clean "all-levels matched" output (inner join semantics).
/// Inherits from <see cref="ThreeLevelJoinStrategy"/> which owns the shared repository setup.
/// </summary>
public sealed class ThreeLevelInnerJoinStrategy : ThreeLevelJoinStrategy
{
    public ThreeLevelInnerJoinStrategy(
        IDatabaseClient databaseClient,
        IApplicationLogger logger,
        SqlQueryParser queryParser)
        : base(databaseClient, logger, queryParser) { }

    public override async Task ExecuteAsync(StrategyExecutionContext context)
    {
        QueryExecutionRequest req = context.Request;
        ParentRepository = DatabaseClient.GetRepository<JObject>(req.ParentContainerName, context.DatabaseName);
        SecondRepository = DatabaseClient.GetRepository<JObject>(req.SecondContainerName!, context.DatabaseName);
        ThirdRepository = DatabaseClient.GetRepository<JObject>(req.ThirdContainerName!, context.DatabaseName);

        string[] parentHeaders = QueryParser.ExtractColumnHeaders(req.ParentQuery);
        string[] secondHeaders = QueryParser.ExtractColumnHeaders(req.SecondQuery!);
        string[] thirdHeaders = QueryParser.ExtractColumnHeaders(req.ThirdQuery!);

        IStorageWriter parentWriter = context.StorageFactory.CreateWriter("ParentResult");
        parentWriter.WriteHeaders(parentHeaders);

        IStorageWriter secondWriter = context.StorageFactory.CreateWriter("SecondLevelResult");
        secondWriter.WriteHeaders(secondHeaders);

        IStorageWriter thirdWriter = context.StorageFactory.CreateWriter("ThirdLevelResult");
        thirdWriter.WriteHeaders(thirdHeaders);

        Logger.LogToConsole($"Getting count from '{req.ParentContainerName}'...");
        int total = await GetCountAsync(ParentRepository, req.ParentQuery);
        Logger.LogToConsole($"Found {total} parent record(s). Running inner-join fetch...");

        IReadOnlyList<string> secondParamKeys = QueryParser.ExtractParameters(req.SecondQuery!);
        IReadOnlyList<string> thirdParamKeys = QueryParser.ExtractParameters(req.ThirdQuery!);
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
                    IDictionary<string, object> secondParams = BuildParameters(secondParamKeys, parentDoc);
                    IEnumerable<JObject> secondResults = await FetchAllAsync(SecondRepository, req.SecondQuery!, secondParams);

                    // Track second-level docs that have at least one third-level match
                    List<JObject> secondDocsToWrite = new();
                    bool anyThirdFound = false;

                    foreach (JObject secondDoc in secondResults)
                    {
                        IDictionary<string, object> thirdParams = BuildParameters(thirdParamKeys, secondDoc);
                        IEnumerable<JObject> thirdResults = await FetchAllAsync(ThirdRepository!, req.ThirdQuery!, thirdParams);

                        bool thirdFound = thirdResults.Any();
                        if (thirdFound)
                        {
                            anyThirdFound = true;
                            secondDocsToWrite.Add(secondDoc);
                            foreach (JObject thirdDoc in thirdResults)
                                WriteDocument(thirdWriter, thirdHeaders, thirdDoc);
                        }
                    }

                    // Only write parent + second level when at least one full chain was found
                    if (anyThirdFound)
                    {
                        WriteDocument(parentWriter, parentHeaders, parentDoc);
                        foreach (JObject secondDoc in secondDocsToWrite)
                            WriteDocument(secondWriter, secondHeaders, secondDoc);
                    }

                    progress.Tick();
                }
            } while (parentResults.Any() && token is not null);
        }
    }
}
