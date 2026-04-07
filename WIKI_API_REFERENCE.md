# API Reference

Complete reference for key classes, interfaces, and models in DataQueryExplorer.

## Interfaces (Domain Layer)

### IQueryStrategy

```csharp
public interface IQueryStrategy : IDisposable
{
    Task ExecuteAsync(StrategyExecutionContext context);
}
```

Base interface for all query strategies. Implementations:
- `SingleContainerQueryStrategy`
- `TwoLevelJoinStrategy`
- `TwoLevelOrphanFinderStrategy`
- `TwoLevelDuplicateFinderStrategy`
- `ThreeLevelJoinStrategy`
- `ThreeLevelInnerJoinStrategy`

---

### IDatabaseClient

```csharp
public interface IDatabaseClient
{
    Task VerifyConnectionAsync(string endpoint, string key);
    Task<IReadOnlyList<string>> ListDatabasesAsync();
    Task<IReadOnlyList<string>> ListContainersAsync(string databaseName);
    IDatabaseRepository<T> GetRepository<T>(string containerName, string databaseName)
        where T : class;
}
```

Manages Cosmos DB connection and repository creation.

**Implementation:** `CosmosDbClient` (Singleton)

---

### IDatabaseRepository<T>

```csharp
public interface IDatabaseRepository<T> : IDisposable where T : class
{
    Task<IEnumerable<T>> QueryAsync(
        string query,
        IDictionary<string, object>? parameters = null);

    Task<PagedResult<T>> QueryPagedAsync(
        string query,
        string? continuationToken,
        int maxItemCount,
        IDictionary<string, object>? parameters = null);
}
```

Executes queries with and without paging. Default `maxItemCount` is 2000 (per `AppConstants.DefaultMaxItemsPerPage`).

**Implementation:** `CosmosDbRepository<T>`

---

### IStorageWriter

```csharp
public interface IStorageWriter
{
    int NextRow { get; }
    void WriteHeaders(IEnumerable<string> headers);
    int WriteRow(IEnumerable<string?> values);
    void WriteCell(string? value, int row, int column);
}
```

Writes data to a single worksheet.

**Implementation:** `ExcelStorageWriter`

---

### IStorageWriterFactory

```csharp
public interface IStorageWriterFactory : IDisposable
{
    IStorageWriter CreateWriter(string sheetName);
    Task SaveAsync(string filePath);
}
```

Creates multiple worksheet writers and saves to a single Excel file.

**Implementation:** `ExcelStorageWriterFactory` (Creates `XLWorkbook`, Transient)

---

### IProgressReporter

```csharp
public interface IProgressReporter : IDisposable
{
    void Tick();
}
```

Reports progress on long-running operations.

**Implementation:** `ConsoleProgressReporter` (wraps `ShellProgressBar`)

---

### IProgressReporterFactory

```csharp
public interface IProgressReporterFactory
{
    IProgressReporter Create(int totalCount, string label = "Processing...");
}
```

Creates progress reporters.

**Implementation:** `ConsoleProgressReporterFactory`

---

### IApplicationLogger

```csharp
public interface IApplicationLogger : IDisposable
{
    void OpenLog(string logName);
    void LogInfo(string message);
    void LogToConsole(string message);
    void LogError(string message, Exception? exception = null);
    void CloseLog();
}
```

Thread-safe logging to file and console.

**Implementation:** `ConsoleApplicationLogger` (Singleton)

---

## Models (Domain Layer)

### QueryExecutionRequest

```csharp
public sealed class QueryExecutionRequest
{
    public required string ParentContainerName { get; init; }
    public required string ParentQuery { get; init; }

    // Two-level / three-level
    public string? SecondContainerName { get; init; }
    public string? SecondQuery { get; init; }

    // Three-level only
    public string? ThirdContainerName { get; init; }
    public string? ThirdQuery { get; init; }

    // Duplicate finder
    public string? GroupByProperty { get; init; }
    public int GroupByPropertyThreshold { get; init; }

    // Pre-read rows from an input Excel file for parameterised queries
    public IReadOnlyList<IReadOnlyDictionary<string, string>>? ParameterRows { get; init; }
}
```

Input parameters for a query execution. Use `init` properties to ensure immutability after creation.

---

### StrategyExecutionContext

```csharp
public sealed class StrategyExecutionContext
{
    public required QueryExecutionRequest Request { get; init; }
    public required string DatabaseName { get; init; }
    public required IStorageWriterFactory StorageFactory { get; init; }
    public required IProgressReporterFactory ProgressFactory { get; init; }
}
```

Bundles all per-execution dependencies passed to `IQueryStrategy.ExecuteAsync()`.

---

### PagedResult<T>

```csharp
public sealed class PagedResult<T> where T : class
{
    public IEnumerable<T> Items { get; }
    public string? ContinuationToken { get; }

    public PagedResult(IEnumerable<T> items, string? continuationToken)
    {
        Items = items;
        ContinuationToken = continuationToken;
    }
}
```

Result of a paged query. If `ContinuationToken` is non-null, there are more results available.

---

## Enums (Domain Layer)

### QueryStrategyType

```csharp
public enum QueryStrategyType
{
    SingleContainerQuery = 0,
    TwoLevelJoinAllResults = 1,
    TwoLevelJoinOrphansOnly = 2,
    TwoLevelJoinFindDuplicates = 3,
    ThreeLevelJoinAllResults = 4,
    ThreeLevelJoinInnerMatchOnly = 5
}
```

Identifies which query strategy to use.

---

### EnvironmentType

```csharp
public enum EnvironmentType
{
    Development = 0,
    Staging = 1,
    Production = 2
}
```

(Placeholder for future multi-environment support)

---

## Key Classes (Application Layer)

### QueryStrategyBase

```csharp
public abstract class QueryStrategyBase : IQueryStrategy
{
    protected readonly IDatabaseClient DatabaseClient;
    protected readonly IApplicationLogger Logger;
    protected readonly SqlQueryParser QueryParser;

    protected IDatabaseRepository<JObject>? ParentRepository;
    protected IDatabaseRepository<JObject>? SecondRepository;
    protected IDatabaseRepository<JObject>? ThirdRepository;

    public abstract Task ExecuteAsync(StrategyExecutionContext context);

    // Protected helpers for subclasses
    protected async Task<(IEnumerable<JObject> Items, string? Token)> FetchPagedAsync(
        IDatabaseRepository<JObject> repo, string query, string? continuationToken,
        IDictionary<string, object>? parameters = null, int maxItems = AppConstants.DefaultMaxItemsPerPage);

    protected async Task<IEnumerable<JObject>> FetchAllAsync(
        IDatabaseRepository<JObject> repo, string query,
        IDictionary<string, object>? parameters = null);

    protected async Task<int> GetCountAsync(
        IDatabaseRepository<JObject> repo, string query);

    protected static IDictionary<string, object> BuildParameters(
        IEnumerable<string> paramNames, JObject source);

    protected static IDictionary<string, object> BuildParameters(
        IEnumerable<string> paramNames, IReadOnlyDictionary<string, string> row);

    protected static int WriteDocument(IStorageWriter writer, string[] headers, JObject document);

    public void Dispose() { /* disposes repositories */ }
}
```

Abstract base for all query strategies. Provides shared helpers for fetching, parameter building, and writing.

---

### SqlQueryParser

```csharp
public sealed class SqlQueryParser
{
    public string[] ExtractColumnHeaders(string query);
    public IReadOnlyList<string> ExtractParameters(string query);
    public bool HasParameters(string query);
    public string BuildCountQuery(string query);
}
```

Parses SQL queries to extract metadata (columns, parameters) and build variants (count query).

---

### DuplicateDetector

```csharp
public static class DuplicateDetector
{
    public static Dictionary<string, int> GroupAndCount(
        IEnumerable<JObject> documents, string propertyName);

    public static Dictionary<string, int> GetDuplicatesAboveThreshold(
        IEnumerable<JObject> documents, string propertyName, int threshold);
}
```

Groups JSON documents by property and counts occurrences. Used by `TwoLevelDuplicateFinderStrategy`.

---

### QueryStrategyFactory

```csharp
public sealed class QueryStrategyFactory
{
    public IQueryStrategy Create(QueryStrategyType type);
}
```

Factory method to instantiate strategies by enum value. Throws `ArgumentOutOfRangeException` for unknown types.

---

## Key Classes (Infrastructure Layer)

### CosmosDbClient

```csharp
public sealed class CosmosDbClient : IDatabaseClient, IDisposable
{
    private CosmosClient? _cosmosClient;
    
    public async Task VerifyConnectionAsync(string endpoint, string key);
    public async Task<IReadOnlyList<string>> ListDatabasesAsync();
    public async Task<IReadOnlyList<string>> ListContainersAsync(string databaseName);
    public IDatabaseRepository<T> GetRepository<T>(string containerName, string databaseName)
        where T : class;
    public void Dispose();
}
```

**Singleton** manager for Cosmos DB connection. Initializes `CosmosClient` on first connection attempt.

---

### CosmosDbRepository<T>

```csharp
public sealed class CosmosDbRepository<T> : IDatabaseRepository<T> where T : class
{
    private readonly Container _container;
    
    public async Task<IEnumerable<T>> QueryAsync(
        string query, IDictionary<string, object>? parameters = null);
    
    public async Task<PagedResult<T>> QueryPagedAsync(
        string query, string? continuationToken, int maxItemCount,
        IDictionary<string, object>? parameters = null);
    
    public void Dispose();
}
```

**Per-repository instance** wrapping a Cosmos `Container`. Handles paging with continuation tokens.

---

### ExcelStorageWriterFactory

```csharp
public sealed class ExcelStorageWriterFactory : IStorageWriterFactory
{
    private readonly XLWorkbook _workbook = new();
    
    public IStorageWriter CreateWriter(string sheetName);
    public Task SaveAsync(string filePath);
    public void Dispose();
}
```

**Transient** (one per execution). Creates worksheets, saves entire workbook to Excel file.

---

### ExcelStorageWriter

```csharp
internal sealed class ExcelStorageWriter : IStorageWriter
{
    private readonly IXLWorksheet _worksheet;
    private int _nextRow = 1;
    
    public int NextRow => _nextRow;
    public void WriteHeaders(IEnumerable<string> headers);
    public int WriteRow(IEnumerable<string?> values);
    public void WriteCell(string? value, int row, int column);
}
```

Wraps an `IXLWorksheet` from ClosedXML. Internal use only.

---

## Key Classes (Console Layer)

### AppRunner

```csharp
public sealed class AppRunner
{
    public async Task RunAsync();
}
```

Main orchestrator. Coordinates:
1. Cosmos DB connection
2. Database/container selection
3. Output folder validation
4. Query input collection
5. Strategy execution
6. Excel file saving

---

### ConsoleInputCollector

```csharp
public sealed class ConsoleInputCollector
{
    public string PromptEndpoint();
    public string PromptKey();
    public string PromptOutputPath();
    public async Task<string> SelectDatabaseAsync();
    public async Task<string> SelectContainerAsync(string databaseName, string promptTitle);
    public async Task<QueryExecutionRequest> CollectRequestAsync(
        QueryStrategyType type, string databaseName);
}
```

Prompts user for input and builds `QueryExecutionRequest`.

---

### ConsoleSelectorUI

```csharp
public sealed class ConsoleSelectorUI
{
    public string Select(IReadOnlyList<string> items, string title);
}
```

Interactive arrow-key menu for selecting from a list. Press Up/Down/Enter to confirm, Escape to exit.

---

### ConsoleApplicationLogger

```csharp
public sealed class ConsoleApplicationLogger : IApplicationLogger, IDisposable
{
    private StreamWriter? _writer;
    private bool _disposed;
    
    public void OpenLog(string logName);
    public void LogInfo(string message);
    public void LogToConsole(string message);
    public void LogError(string message, Exception? exception = null);
    public void CloseLog();
    public void Dispose();
}
```

**Singleton** logger with thread-safe file + console output. Creates timestamped log files in `./Logs - {logName}/` directory.

---

## Constants (Domain Layer)

### AppConstants

```csharp
public static class AppConstants
{
    public const int DefaultMaxItemsPerPage = 2000;
    public const string CountColumnName = "CountOfResult";
    public const string IsChildFoundColumn = "IsChildFound";
    public const string IsSecondChildFoundColumn = "IsSecondChildFound";
    public const string IsThirdChildFoundColumn = "IsThirdChildFound";
}
```

Configurable constants for paging and Excel column names.

---

## Exceptions

| Exception | When thrown | Handling |
|---|---|---|
| `InvalidOperationException` | Cosmos DB not initialized, missing required fields | Caught in `AppRunner`, logged and re-thrown |
| `ArgumentException` | Invalid container/database name, missing SQL parameters | User input validation |
| `OperationCanceledException` | User presses Escape in menu | Caught in `finally` block, clean exit |
| `ArgumentOutOfRangeException` | Unknown `QueryStrategyType` in factory | Indicates a programming error; should never occur |

---

## Next Steps

- **[Query Types Guide](Query-Types-Guide)** — Detailed examples of each strategy
- **[Architecture](Architecture)** — Design patterns and extension points
- **[Troubleshooting](Troubleshooting)** — Common errors and solutions
