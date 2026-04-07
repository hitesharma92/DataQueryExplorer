namespace DataQueryExplorer.Console.UI;

/// <summary>
/// Collects all console inputs needed to build a <see cref="QueryExecutionRequest"/>
/// and the supporting connection/output configuration.
/// </summary>
public sealed class ConsoleInputCollector
{
    private readonly IDatabaseClient _dbClient;
    private readonly ConsoleSelectorUI _selectorUI;
    private readonly SqlQueryParser _queryParser;
    private readonly InputExcelReader _excelReader;
    private readonly IApplicationLogger _logger;

    public ConsoleInputCollector(
        IDatabaseClient dbClient,
        ConsoleSelectorUI selectorUI,
        SqlQueryParser queryParser,
        InputExcelReader excelReader,
        IApplicationLogger logger)
    {
        _dbClient = dbClient;
        _selectorUI = selectorUI;
        _queryParser = queryParser;
        _excelReader = excelReader;
        _logger = logger;
    }

    public string PromptEndpoint()
        => Prompt("\nPlease provide the database endpoint URL (starts with https://):\n");

    public string PromptKey()
        => Prompt("\nPlease provide the read/write key:\n");

    public string PromptOutputPath()
        => Prompt("\nPlease provide the output folder path (e.g. C:\\Output\\):\n");

    /// <summary>
    /// Lists databases from the live connection and lets the user select one.
    /// If only one database exists it is auto-selected.
    /// </summary>
    public async Task<string> SelectDatabaseAsync()
    {
        _logger.LogToConsole("Fetching databases...");
        IReadOnlyList<string> databases = await _dbClient.ListDatabasesAsync();

        if (databases.Count == 0)
            throw new InvalidOperationException("No databases found. Verify your endpoint and key.");

        return _selectorUI.Select(databases, "Select Database");
    }

    /// <summary>
    /// Lists containers in the given database and lets the user select one.
    /// </summary>
    public async Task<string> SelectContainerAsync(string databaseName, string promptTitle)
    {
        _logger.LogToConsole("Fetching containers...");
        IReadOnlyList<string> containers = await _dbClient.ListContainersAsync(databaseName);

        if (containers.Count == 0)
            throw new InvalidOperationException($"No containers found in database '{databaseName}'.");

        return _selectorUI.Select(containers, promptTitle);
    }

    /// <summary>
    /// Collects all query inputs for the given strategy and builds a <see cref="QueryExecutionRequest"/>.
    /// Handles parameterised queries by prompting for an input Excel file when required.
    /// </summary>
    public async Task<QueryExecutionRequest> CollectRequestAsync(QueryStrategyType type, string databaseName)
    {
        return type switch
        {
            QueryStrategyType.SingleContainerQuery => await CollectSingleAsync(databaseName),
            QueryStrategyType.TwoLevelJoinAllResults => await CollectTwoLevelAsync(databaseName),
            QueryStrategyType.TwoLevelJoinOrphansOnly => await CollectTwoLevelAsync(databaseName),
            QueryStrategyType.TwoLevelJoinFindDuplicates => await CollectTwoLevelDuplicatesAsync(databaseName),
            QueryStrategyType.ThreeLevelJoinAllResults => await CollectThreeLevelAsync(databaseName),
            QueryStrategyType.ThreeLevelJoinInnerMatchOnly => await CollectThreeLevelAsync(databaseName),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    // ------------------------------------------------------------------
    // Private collection methods
    // ------------------------------------------------------------------

    private async Task<QueryExecutionRequest> CollectSingleAsync(string db)
    {
        string container = await SelectContainerAsync(db, "Select Container to Query");
        string query = Prompt($"Enter query.\nExample: SELECT c.id, c.name FROM c WHERE c.type = 'user'\n");
        IReadOnlyList<IReadOnlyDictionary<string, string>>? paramRows = await TryReadParameterExcelAsync(query);

        return new QueryExecutionRequest
        {
            ParentContainerName = container,
            ParentQuery = query,
            ParameterRows = paramRows
        };
    }

    private async Task<QueryExecutionRequest> CollectTwoLevelAsync(string db)
    {
        string parentContainer = await SelectContainerAsync(db, "Select Parent Container");
        string parentQuery = Prompt("Enter parent query.\nExample: SELECT c.id, c.code FROM c WHERE c.type = 'order'\n");

        string childContainer = await SelectContainerAsync(db, "Select Child Container");
        string childQuery = Prompt(
            "Enter child query.\n" +
            "Note: Use @paramName for foreign key.  Example:\n" +
            "SELECT c.id, c.detail FROM c WHERE c.order_id = @id\n");

        return new QueryExecutionRequest
        {
            ParentContainerName = parentContainer,
            ParentQuery = parentQuery,
            SecondContainerName = childContainer,
            SecondQuery = childQuery
        };
    }

    private async Task<QueryExecutionRequest> CollectTwoLevelDuplicatesAsync(string db)
    {
        QueryExecutionRequest req = await CollectTwoLevelAsync(db);
        string groupBy = Prompt("Enter the property name to group by for duplicate detection (e.g. id, code):\n");
        int threshold = int.Parse(Prompt("Enter the duplicate threshold — values with count GREATER than this will be flagged:\n"));

        return new QueryExecutionRequest
        {
            ParentContainerName = req.ParentContainerName,
            ParentQuery = req.ParentQuery,
            SecondContainerName = req.SecondContainerName,
            SecondQuery = req.SecondQuery,
            GroupByProperty = groupBy,
            GroupByPropertyThreshold = threshold
        };
    }

    private async Task<QueryExecutionRequest> CollectThreeLevelAsync(string db)
    {
        string parentContainer = await SelectContainerAsync(db, "Select Parent Container");
        string parentQuery = Prompt("Enter parent query.\nExample: SELECT c.id, c.code FROM c\n");

        string secondContainer = await SelectContainerAsync(db, "Select Second-Level Container");
        string secondQuery = Prompt(
            "Enter second-level query.\n" +
            "Example: SELECT c.id, c.detail FROM c WHERE c.parent_id = @id\n");

        string thirdContainer = await SelectContainerAsync(db, "Select Third-Level Container");
        string thirdQuery = Prompt(
            "Enter third-level query.\n" +
            "Example: SELECT c.id, c.extra FROM c WHERE c.detail_id = @id\n");

        return new QueryExecutionRequest
        {
            ParentContainerName = parentContainer,
            ParentQuery = parentQuery,
            SecondContainerName = secondContainer,
            SecondQuery = secondQuery,
            ThirdContainerName = thirdContainer,
            ThirdQuery = thirdQuery
        };
    }

    // ------------------------------------------------------------------
    // Helpers
    // ------------------------------------------------------------------

    private async Task<IReadOnlyList<IReadOnlyDictionary<string, string>>?> TryReadParameterExcelAsync(string query)
    {
        if (!_queryParser.HasParameters(query)) return null;

        IReadOnlyList<string> paramKeys = _queryParser.ExtractParameters(query);
        _logger.LogToConsole($"\nQuery contains SQL parameters: {string.Join(", ", paramKeys)}");

        while (true)
        {
            string path = Prompt(
                $"Provide path to Excel file with column names matching: {string.Join(", ", paramKeys)}\n" +
                "Example: C:\\Inputs\\MyParameters.xlsx\n");

            if (!File.Exists(path))
            {
                _logger.LogToConsole($"File not found: {path}  — please try again.");
                continue;
            }

            IReadOnlyList<string> headers = _excelReader.ReadHeaders(path);
            List<string> missing = paramKeys.Where(p => !headers.Contains(p, StringComparer.OrdinalIgnoreCase)).ToList();
            if (missing.Count > 0)
            {
                _logger.LogToConsole($"Missing columns in Excel: {string.Join(", ", missing)}  — please fix and retry.");
                continue;
            }

            IReadOnlyList<IReadOnlyDictionary<string, string>> rows = _excelReader.ReadRows(path);
            if (rows.Count == 0)
            {
                _logger.LogToConsole("No data rows found in the file. Please add data and retry.");
                continue;
            }

            _logger.LogToConsole($"Read {rows.Count} row(s) from input Excel.");
            return rows;
        }
    }

    private static string Prompt(string message)
    {
        System.Console.WriteLine($"\n{message}");
        return System.Console.ReadLine() ?? string.Empty;
    }
}
