# Architecture

DataQueryExplorer follows Clean Architecture principles with a clear separation of concerns across 4 layers.

## Layered Architecture

```
┌─────────────────────────────────────────────────┐
│  Console Layer (DataQueryExplorer.Console)      │
│  UI, Logging, Orchestration, Dependency Injection
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Application Layer (DataQueryExplorer.Application)
│  Query Strategies, SQL Parsing, Utilities        │
│  (No external dependencies except Domain)
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Infrastructure Layer                           │
│  (DataQueryExplorer.Infrastructure)             │
│  Cosmos DB SDK Wrapper, Excel I/O               │
└─────────────────────────────────────────────────┘
                        ↓
┌─────────────────────────────────────────────────┐
│  Domain Layer (DataQueryExplorer.Domain)        │
│  Models, Interfaces, Enums, Constants           │
│  (No dependencies on other layers)
└─────────────────────────────────────────────────┘
```

## Layer Details

### Domain Layer (Core)

**Responsibilities:**
- Define data models (requests, results, execution context)
- Define all service interfaces (contracts)
- Enum definitions (query strategy types, environment types)
- Constants and shared utilities

**Key files:**
- `Models/QueryExecutionRequest.cs` — Input parameters for a query execution
- `Models/PagedResult<T>.cs` — Paging response (items + continuation token)
- `Models/StrategyExecutionContext.cs` — Bundles all per-execution dependencies
- `Interfaces/IQueryStrategy.cs` — Interface all strategies implement
- `Interfaces/IDatabaseClient.cs` — Cosmos DB abstraction
- `Interfaces/IStorageWriter.cs` — Excel output abstraction
- `Enums/QueryStrategyType.cs` — 6 supported query types

**Dependencies:** None (lowest layer)

---

### Infrastructure Layer

**Responsibilities:**
- Wrap Cosmos DB SDK (connection, paging, querying)
- Implement Excel writing via ClosedXML
- Handle resource lifecycle (connections, file handles)

**Key classes:**
- `CosmosDbClient` — Singleton manager for CosmosClient, initializes connection
- `CosmosDbRepository<T>` — Generic repository with `QueryAsync` and `QueryPagedAsync`
  - Implements paging logic
  - Builds QueryDefinition from SQL + parameters
  - Handles continuation tokens
- `ExcelStorageWriterFactory` — Creates worksheets, manages XLWorkbook
- `ExcelStorageWriter` — Wraps IXLWorksheet, writes headers/rows
- `InputExcelReader` — Reads parameter rows from Excel file

**Dependency Injection:**
- `CosmosDbClient` → Singleton (SDK recommends one per app lifetime)
- `IStorageWriterFactory` (ExcelStorageWriterFactory) → Transient (one workbook per run)

**Key patterns:**
- **Repository pattern:** `IDatabaseRepository<T>` abstraction
- **Factory pattern:** `IStorageWriterFactory.CreateWriter()` for sheet creation
- **Paging:** `FeedIterator<T>` wrapped with continuation token management

---

### Application Layer

**Responsibilities:**
- Implement query strategies (the 6 different join types)
- Parse SQL queries (extract columns, parameters)
- Orchestrate parent-child-grandchild fetches

**Key classes:**
- `QueryStrategyBase` — Abstract base for all strategies
  - Provides helpers: `FetchPagedAsync`, `FetchAllAsync`, `GetCountAsync`, `BuildParameters`, `WriteDocument`
  - Manages repositories (parent, second, third)
  - Implements repository disposal

- Strategy concrete classes (inherit from QueryStrategyBase):
  - `SingleContainerQueryStrategy`
  - `TwoLevelJoinStrategy`
  - `TwoLevelOrphanFinderStrategy`
  - `TwoLevelDuplicateFinderStrategy`
  - `ThreeLevelJoinStrategy`
  - `ThreeLevelInnerJoinStrategy`

- Utilities:
  - `SqlQueryParser` — Extract headers, parameters; build count query
  - `DuplicateDetector` — Group documents, count occurrences, filter by threshold
  - `QueryStrategyFactory` — Create strategy instances by type

**Key patterns:**
- **Strategy pattern:** Each query type is a separate strategy class
- **Template method:** `QueryStrategyBase` defines the flow skeleton; subclasses override `ExecuteAsync`
- **Factory pattern:** `QueryStrategyFactory` maps enum values to instances

**Dependency Injection:**
- Strategies → Transient (hold per-run repository instances)
- `SqlQueryParser` → Singleton (stateless, reusable)

---

### Console Layer

**Responsibilities:**
- Orchestrate the entire execution (AppRunner)
- Collect user input (ConsoleInputCollector)
- Manage logging (ConsoleApplicationLogger)
- Display menus and progress (ConsoleMenu, ConsoleSelectorUI, ConsoleProgressReporterFactory)
- Set up dependency injection
- Handle application lifecycle

**Key classes:**
- `Program.cs` — Entry point, DI composition root, exception handling
- `AppRunner` — Main orchestrator
  1. Connects to Cosmos DB
  2. Selects database
  3. Validates output folder
  4. Selects query strategy
  5. Collects query inputs
  6. Executes strategy
  7. Saves Excel output

- `ConsoleInputCollector` — Prompts for endpoints, databases, queries, Excel files
- `ConsoleSelectorUI` — Arrow-key menu for selecting from lists
- `ConsoleMenu` — Strategy type selection menu
- `ConsoleApplicationLogger` — Thread-safe file + console logging with disposal guards
- `ConsoleProgressReporterFactory` — ShellProgressBar wrapper for visual feedback

**Dependency Injection Container:**
```csharp
// Singletons (app lifetime)
services.AddSingleton<IApplicationLogger, ConsoleApplicationLogger>();
services.AddSingleton<IDatabaseClient, CosmosDbClient>();
services.AddSingleton<SqlQueryParser>();
... (other singletons)

// Transient (one per request)
services.AddTransient<IStorageWriterFactory, ExcelStorageWriterFactory>();
services.AddTransient<SingleContainerQueryStrategy>();
... (other strategies)
```

**Key patterns:**
- **Composition root:** All services registered in one place (Program.cs)
- **Singleton pattern:** Cosmos client, logger, parser (shared, stateless or managed state)
- **Transient pattern:** Factories and strategies (fresh instances per run)

---

## Data Flow Diagram

```
┌──────────────────────────────────────┐
│ User Input (Console)                 │
│ - Endpoint, Key, Database            │
│ - Container(s), Query(ies)           │
│ - Output path, Strategy type         │
└──────────────────────────────────────┘
          ↓
┌──────────────────────────────────────┐
│ AppRunner.RunAsync()                 │
│ - Validates connection               │
│ - Creates StrategyExecutionContext   │
│ - Instantiates Strategy              │
└──────────────────────────────────────┘
          ↓
┌──────────────────────────────────────┐
│ Strategy.ExecuteAsync(context)       │
│ - Reads strategy.Request             │
│ - Fetches from parent container      │
│ - For each parent, fetch child(ren)  │
│ - Writes to IStorageWriter sheets    │
│ - Returns when done                  │
└──────────────────────────────────────┘
          ↓
┌──────────────────────────────────────┐
│ storageFactory.SaveAsync(path)       │
│ - Flushes all sheets                 │
│ - Writes Excel file to disk          │
└──────────────────────────────────────┘
          ↓
┌──────────────────────────────────────┐
│ finally { logger.CloseLog() }        │
│ - Flushes log file                   │
│ - Closes all resources               │
│ - Disposes ServiceProvider           │
└──────────────────────────────────────┘
```

---

## Key Design Decisions

### 1. Strategy Pattern for Query Types

**Why:** Each join type has unique logic but shares common infrastructure (paging, parameter building, writing).

**Benefit:** New query types can be added by inheriting from `QueryStrategyBase` without modifying existing code.

---

### 2. Repository Abstraction (IDatabaseRepository<T>)

**Why:** Isolates Cosmos SDK specifics from application logic.

**Benefit:** Could swap Cosmos DB for another data source by implementing `IDatabaseRepository<T>` without changing strategies.

---

### 3. Global Usings & Explicit Types

**Why:** Reduces boilerplate and improves code clarity.

**Benefit:** Every type is clear at a glance; no ambiguity with `var`; easier to refactor.

---

### 4. Singleton Cosmos Client in DI Container

**Why:** Cosmos SDK recommends one `CosmosClient` per app lifetime.

**Benefit:** Connection pooling, automatic retry policies, cost efficiency.

---

### 5. Transient IStorageWriterFactory

**Why:** Each execution creates a new `XLWorkbook` (Excel file in memory).

**Benefit:** No cross-run contamination; each run gets a clean workbook.

---

### 6. Resource Disposal with Using Blocks

**Why:** Strategies, factories, and logger must be disposed to release files, streams, and connections.

**Benefit:** No resource leaks; `finally` blocks and `await using (provider)` ensure cleanup even on exceptions.

---

## Extension Points

### Add a New Query Strategy

1. Create `newStrategy.cs` in `Strategies/` folder:
   ```csharp
   public sealed class YourNewStrategy : QueryStrategyBase { ... }
   ```

2. Implement `ExecuteAsync(StrategyExecutionContext context)`:
   - Use inherited helpers (`FetchPagedAsync`, `BuildParameters`, `WriteDocument`)
   - Write custom logic for your join pattern

3. Add to DI in `Program.cs`:
   ```csharp
   services.AddTransient<YourNewStrategy>();
   ```

4. Add enum value to `QueryStrategyType`:
   ```csharp
   YourNewQuery = 7  // new value
   ```

5. Update `QueryStrategyFactory.Create()`:
   ```csharp
   QueryStrategyType.YourNewQuery => ...GetRequiredService<YourNewStrategy>()
   ```

6. Update `ConsoleMenu` to show the new option.

### Change Storage Backend

1. Implement `IStorageWriter` for your format (CSV, JSON, etc.)
2. Create factory `IStorageWriterFactory` implementation
3. Update DI registration:
   ```csharp
   services.AddTransient<IStorageWriterFactory, YourStorageFactory>();
   ```
4. Strategies run unchanged; they only know about `IStorageWriter` abstraction.

---

## Testing Strategy

### Unit Tests

- **Domain layer:** No external dependencies; fast tests on models
- **Application layer:** Strategies mocked with `NSubstitute` for isolated testing
- **Infrastructure layer:** Excel writing, stream management tested in isolation
- **Console layer:** Logging, DI, menu logic tested with mocks

### Test Files

- `SqlQueryParserTests.cs` — SQL parsing correctness
- `DuplicateDetectorTests.cs` — Grouping and counting logic
- `ExcelStorageWriterFactoryTests.cs` — Excel output format
- `ConsoleApplicationLoggerTests.cs` — Disposal safety, thread safety
- `QueryStrategyFactoryTests.cs` — Strategy resolution, enum validation
- `StrategyGuardTests.cs` — Guard conditions (null checks, error paths)

**Run tests:**
```bash
dotnet test
```

---

## Next Steps

- **[Installation & Setup](Installation-&-Setup)** — Build and run the project
- **[Query Types Guide](Query-Types-Guide)** — Understand each strategy
- **[API Reference](API-Reference)** — Detailed class documentation
- **[Contributing](Contributing)** — Contribute improvements
