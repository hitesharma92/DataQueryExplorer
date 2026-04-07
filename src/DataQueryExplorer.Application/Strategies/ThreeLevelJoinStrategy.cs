namespace DataQueryExplorer.Application.Strategies;

/// <summary>
/// Three-container left-join query (outer join semantics).
/// Parent → SecondChild → ThirdChild, writing all levels to separate worksheets.
/// Each level's sheet has a boolean column indicating whether the next level was found.
/// </summary>
public class ThreeLevelJoinStrategy : QueryStrategyBase
{
    public ThreeLevelJoinStrategy(
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

        int isSecondFoundCol = parentHeaders.Length + 1;
        int isThirdFoundCol = secondHeaders.Length + 1;

        Logger.LogToConsole($"Getting count from '{req.ParentContainerName}'...");
        int total = await GetCountAsync(ParentRepository, req.ParentQuery);
        Logger.LogToConsole($"Found {total} parent record(s). Fetching three-level join...");

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
                    int parentRow = WriteDocument(parentWriter, parentHeaders, parentDoc);

                    IDictionary<string, object> secondParams = BuildParameters(secondParamKeys, parentDoc);
                    IEnumerable<JObject> secondResults = await FetchAllAsync(SecondRepository, req.SecondQuery!, secondParams);

                    bool secondFound = false;
                    foreach (JObject secondDoc in secondResults)
                    {
                        secondFound = true;
                        int secondRow = WriteDocument(secondWriter, secondHeaders, secondDoc);

                        IDictionary<string, object> thirdParams = BuildParameters(thirdParamKeys, secondDoc);
                        IEnumerable<JObject> thirdResults = await FetchAllAsync(ThirdRepository!, req.ThirdQuery!, thirdParams);

                        bool thirdFound = false;
                        foreach (JObject thirdDoc in thirdResults)
                        {
                            thirdFound = true;
                            WriteDocument(thirdWriter, thirdHeaders, thirdDoc);
                        }
                        secondWriter.WriteCell(thirdFound.ToString(), secondRow, isThirdFoundCol);
                    }

                    parentWriter.WriteCell(secondFound.ToString(), parentRow, isSecondFoundCol);
                    progress.Tick();
                }
            } while (parentResults.Any() && token is not null);
        }
    }
}
