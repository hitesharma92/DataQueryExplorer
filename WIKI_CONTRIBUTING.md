# Contributing

Thank you for your interest in DataQueryExplorer! Here's how to contribute.

## Code of Conduct

Be respectful, inclusive, and professional. The project welcomes contributions from all backgrounds.

---

## Getting Started

### 1. Fork & Clone

```bash
git clone https://github.com/your-username/DataQueryExplorer.git
cd DataQueryExplorer
```

### 2. Create a Feature Branch

```bash
git checkout -b feature/your-feature-name
```

Follow [Git branch naming conventions](Version-Control-and-Git-Management):
- `feature/` — new features
- `fix/` — bug fixes
- `docs/` — documentation
- `refactor/` — code refactoring without behavior change

### 3. Make Your Changes

Code style:
- ✅ Explicit types (no `var`)
- ✅ Global usings (leverage `GlobalUsings.cs`)
- ✅ XML doc comments on public APIs
- ✅ `sealed` on concrete classes (not meant for inheritance)
- ✅ `static` on pure utility methods  
- ✅ Proper `IDisposable` impl with disposal checks

Example:

```csharp
/// <summary>
/// Validates a Cosmos DB query syntax.
/// This is a pure utility method with no side effects.
/// </summary>
/// <param name="query">SQL query string to validate.</param>
/// <returns>true if the query is syntactically valid; false otherwise.</returns>
public static bool ValidateQuery(string query)
{
    // ...implementation...
}
```

### 4. Write Tests

Every feature or fix should include tests.

**Test file naming:** `[FeatureName]Tests.cs`

**Example:**
```csharp
public sealed class YourFeatureTests
{
    [Fact]
    public void YourFeature_WithInput_ReturnsExpectedOutput()
    {
        // Arrange
        var input = new QueryExecutionRequest { /* ... */ };
        var target = new YourClass();

        // Act
        var result = target.YourMethod(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("expected", result.Value);
    }

    [Fact]
    public void YourFeature_WithInvalidInput_ThrowsArgumentException()
    {
        var invalidInput = null;
        var target = new YourClass();

        Assert.Throws<ArgumentException>(() => target.YourMethod(invalidInput));
    }
}
```

**Run tests locally:**
```bash
dotnet test
```

All tests must pass before submitting a PR.

---

## Code Review Standards

Your PR will be reviewed for:

### Correctness
- ✅ No null reference exceptions (`NullReferenceException`)
- ✅ Proper disposal of `IDisposable` resources
- ✅ Exception handling is explicit, not suppressed
- ✅ Edge cases handled (empty lists, null inputs, etc.)

### Robustness (Critical Issues from Recent Review)
- ✅ Resource disposal: Services obtained via DI must be wrapped in `using` blocks if Transient
- ✅ Null guards at entry points: Check required parameters at method start, throw clear exceptions
- ✅ No performance anti-patterns: No `GC.Collect()` in hot loops, no unmanaged memory leaks

### Code Quality
- ✅ No `var` — all types explicit
- ✅ No `using var` — explicit `using()` blocks
- ✅ Names are clear and descriptive
- ✅ Methods are single-responsibility
- ✅ Comments explain *why*, not *what* (code explains what)

### Architecture
- ✅ Changes respect Clean Architecture layering (Domain → Application → Infrastructure → Console)
- ✅ Interfaces are used for external dependencies (Cosmos SDK, file I/O)
- ✅ No circular dependencies between layers
- ✅ New strategies inherit from `QueryStrategyBase`, not duplicating logic

### Testing
- ✅ New code has corresponding unit tests
- ✅ Tests are not mocking internals; they test behavior
- ✅ Test names describe the scenario and expected outcome
- ✅ `NSubstitute` used for interface mocking, not concrete classes

### Documentation
- ✅ XML doc comments on public APIs
- ✅ Complex logic explained with inline comments
- ✅ Updated README or wiki if user-facing behavior changed

---

## Types of Contributions

### Bug Fixes

**Format:**
```
feat(component): Fix issue with X

- Problem: Brief description of the bug
- Solution: How you fixed it
- Tested: Describe test case

Fixes #123
```

**Example:**
```
fix(AppRunner): Wrap transient services in using blocks

- Problem: IStorageWriterFactory and IQueryStrategy were obtained but never disposed,
  leaking XLWorkbook and repository resources.
- Solution: Added nested using blocks to ensure disposal before exiting RunAsync.
- Tested: Verified with ExcelStorageWriterFactoryTests that Dispose() is called.

Fixes #15
```

---

### Features

**Format:**
```
feat(component): Add new feature name

- What: Brief description of the feature
- Why: Use case or problem solved
- How: Implementation approach
- Tests: List of test scenarios covered

Relates to #abc
```

**Example:**
```
feat(strategies): Add four-level join query strategy

- What: New `FourLevelJoinStrategy` for querying across 4 containers.
- Why: Support for deeply nested data hierarchies in denormalized schemas.
- How: Inherit from QueryStrategyBase, implement ExecuteAsync with 4-level paging loop.
- Tests: 
  - FourLevelJoinStrategy_AllResults_WritesAllLevels
  - FourLevelJoinStrategy_MissingGrandGrandChild_StillWritesParent

Relates to #20
```

---

### Refactoring (No Behavioral Change)

**Format:**
```
refactor(component): Improve code clarity

- What: Description of refactoring
- Why: Benefits (performance, readability, maintainability)
- Impact: No external behavior change
```

**Example:**
```
refactor(strategies): Extract common pagination logic into base class

- What: Move FetchPagedAsync implementation to QueryStrategyBase to avoid duplication.
- Why: Single source of truth for paging logic; easier to maintain and test.
- Impact: No visible behavior change; all strategies use same logic.
```

---

## Pull Request Checklist

Before submitting:

- [ ] Branch is up-to-date with `main`: `git pull origin main`
- [ ] Code compiles: `dotnet build`
- [ ] All tests pass: `dotnet test` (43+ passing)
- [ ] No warnings: Build output shows `0 Warning(s)`
- [ ] Commit message is clear and follows conventions
- [ ] New public APIs have XML doc comments
- [ ] No `var` declarations introduced
- [ ] No breaking changes (or documented in commit)

---

## Commit Message Convention

Follow [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <subject>

<body>

<footer>
```

**Types:**
- `feat` — New feature
- `fix` — Bug fix
- `docs` — Documentation
- `refactor` — Code refactoring (no behavior change)
- `test` — Test additions/changes
- `chore` — Dependencies, build tooling

**Scope:** Component affected (e.g., `strategies`, `logging`, `cosmosdb`, `excel`)

**Subject:** Imperative mood, lowercase, no period
- ✅ "add null guard for GroupByProperty"
- ❌ "added null guard" or "Adds null guard"

**Body:** Explain *why*, not *what*. Link related issues.

**Footer:** `Fixes #123` or `Related to #456`

**Examples:**
```
fix(AppRunner): Wrap transient services in using blocks

The IStorageWriterFactory and IQueryStrategy instances obtained via
GetRequiredService are Transient and not disposed by the DI container.
This caused resource leaks in XLWorkbook and repository cleanup.

Fixes #15
```

```
feat(api): Add IQueryStrategy.ValidateAsync method

Allows strategies to validate input before execution starts, catching
configuration errors early and providing clearer error messages to users.

Related to #20
```

---

## Extension Guidelines

### Adding a New Query Strategy

1. **Create file:** `src/DataQueryExplorer.Application/Strategies/YourStrategyName.cs`

2. **Inherit from `QueryStrategyBase`:**
   ```csharp
   public sealed class YourStrategyName : QueryStrategyBase
   {
       public YourStrategyName(
           IDatabaseClient databaseClient,
           IApplicationLogger logger,
           SqlQueryParser queryParser)
           : base(databaseClient, logger, queryParser) { }

       public override async Task ExecuteAsync(StrategyExecutionContext context)
       {
           // Implement your join logic here
           // Use inherited methods: FetchPagedAsync, BuildParameters, WriteDocument
       }
   }
   ```

3. **Add enum value:** `src/DataQueryExplorer.Domain/Enums/QueryStrategyType.cs`
   ```csharp
   public enum QueryStrategyType
   {
       // ... existing values ...
       YourNewQuery = 6
   }
   ```

4. **Register in DI:** `src/DataQueryExplorer.Console/Program.cs`
   ```csharp
   services.AddTransient<YourStrategyName>();
   ```

5. **Update factory:** `src/DataQueryExplorer.Application/Factories/QueryStrategyFactory.cs`
   ```csharp
   public IQueryStrategy Create(QueryStrategyType type) => type switch
   {
       // ... existing cases ...
       QueryStrategyType.YourNewQuery => 
           _serviceProvider.GetRequiredService<YourStrategyName>(),
       // ...
   };
   ```

6. **Update menu:** `src/DataQueryExplorer.Console/UI/ConsoleMenu.cs`
   ```csharp
   (QueryStrategyType.YourNewQuery, "7. Your query description"),
   ```

7. **Write tests:** `tests/DataQueryExplorer.Tests/StrategyYourNameTests.cs`

8. **Update documentation:** Add details to [Query Types Guide](Query-Types-Guide)

---

## Performance Considerations

### Pagination
- Default page size is 2000 items per Cosmos request (edit `AppConstants.DefaultMaxItemsPerPage`)
- Smaller pages = more requests but less memory per request
- Test with your typical dataset size

### Memory
- Results are held in memory before Excel export
- Large datasets may require filtering or batching

### RUs (Request Units)
- Each Cosmos query consumes RUs based on data scanned
- Multi-level joins multiply query count (1 parent × N children × M grandchildren = 1 + N + M + NM queries)
- Optimize with WHERE clauses

---

## Testing Best Practices

### Unit Test Structure

```csharp
[Theory]
[InlineData(QueryStrategyType.SingleContainerQuery)]
[InlineData(QueryStrategyType.TwoLevelJoinAllResults)]
public void Strategy_WithValidInput_ReturnsResults(QueryStrategyType type)
{
    // Arrange
    IDatabaseClient client = Substitute.For<IDatabaseClient>();
    IApplicationLogger logger = Substitute.For<IApplicationLogger>();
    var strategy = CreateStrategy(type, client, logger);
    var context = BuildContext(/* ... */);

    // Act
    var result = Record.Exception(
        () => strategy.ExecuteAsync(context).GetAwaiter().GetResult());

    // Assert
    Assert.Null(result); // No exception
}
```

### What to Test
- Happy path (valid input → expected output)
- Error cases (null/invalid input → thrown exception)
- Boundary conditions (empty list, single item, large dataset)
- Resource cleanup (disposed resources don't leak)

### What NOT to Test
- Internal implementation details (private methods)
- Third-party libraries (trust ClosedXML, NSubstitute, etc. work correctly)
- Cosmos DB SDK behavior (assume it handles queries correctly)

---

## Questions?

- **[Architecture](Architecture)** — Design patterns and extension points
- **[API Reference](API-Reference)** — Class and method documentation
- Open an issue on GitHub: https://github.com/hitesharma92/DataQueryExplorer/issues

---

## License

By contributing, you agree to license your changes under the same license as the project (see LICENSE file).

---

**Thank you for contributing to DataQueryExplorer!** 🎉
