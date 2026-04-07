namespace DataQueryExplorer.Application.Strategies;

/// <summary>
/// Two-container left-join query.
/// For every parent document, runs the child query (substituting @params from the parent).
/// Both parent and child results are written to separate worksheets.
/// A boolean <c>IsChildFound</c> column is appended to the parent sheet.
/// </summary>
public sealed class TwoLevelJoinStrategy : QueryStrategyBase
{
    public TwoLevelJoinStrategy(
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
        parentWriter.WriteHeaders([.. parentHeaders, AppConstants.IsChildFoundColumn]);

        IStorageWriter childWriter = context.StorageFactory.CreateWriter("ChildResult");
        childWriter.WriteHeaders(childHeaders);

        int isChildFoundColumn = parentHeaders.Length + 1;

        Logger.LogToConsole($"Getting count from '{req.ParentContainerName}'...");
        int total = await GetCountAsync(ParentRepository, req.ParentQuery);
        Logger.LogToConsole($"Found {total} parent record(s). Fetching...");

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
                    int parentRow = WriteDocument(parentWriter, parentHeaders, parentDoc);

                    IDictionary<string, object> childParams = BuildParameters(childParamKeys, parentDoc);
                    IEnumerable<JObject> childResults = await FetchAllAsync(SecondRepository, req.SecondQuery!, childParams);

                    bool childFound = false;
                    foreach (JObject childDoc in childResults)
                    {
                        childFound = true;
                        WriteDocument(childWriter, childHeaders, childDoc);
                    }

                    parentWriter.WriteCell(childFound.ToString(), parentRow, isChildFoundColumn);
                    progress.Tick();
                }
            } while (parentResults.Any() && token is not null);
        }
    }
}
