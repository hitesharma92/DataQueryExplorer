using DataQueryExplorer.Application.Services;
using DataQueryExplorer.Application.Strategies;
using DataQueryExplorer.Domain;
using DataQueryExplorer.Domain.Interfaces;
using NSubstitute;

namespace DataQueryExplorer.Tests;

/// <summary>
/// Tests for strategy-level guard conditions.
///
/// Covers gaps identified in code review:
/// - CR-04: <c>TwoLevelDuplicateFinderStrategy</c> used null-forgiving <c>!</c> on
///   <c>GroupByProperty</c> without a preceding null guard. A missing property would
///   propagate deep into <see cref="Application.Utilities.DuplicateDetector"/> and
///   throw a confusing NullReferenceException. After the CR-04 fix, the strategy
///   throws a clear <see cref="InvalidOperationException"/> at entry.
///
/// - CR-03 (documented, not fixed here): <c>CosmosDbRepository.QueryPagedAsync</c>
///   subtracts <c>results.Count</c> (cumulative) instead of the per-batch count.
///   The paging accumulation bug is illustrated via XML doc below.
/// </summary>
public sealed class StrategyGuardTests
{
    // -----------------------------------------------------------------------
    // CR-04: TwoLevelDuplicateFinderStrategy — null GroupByProperty guard
    // -----------------------------------------------------------------------

    [Fact]
    public async Task TwoLevelDuplicateFinderStrategy_NullGroupByProperty_ThrowsInvalidOperationException()
    {
        // Arrange
        IDatabaseClient dbClient = Substitute.For<IDatabaseClient>();
        IApplicationLogger logger = Substitute.For<IApplicationLogger>();
        SqlQueryParser parser = new();

        TwoLevelDuplicateFinderStrategy strategy = new(dbClient, logger, parser);

        StrategyExecutionContext context = new()
        {
            Request = new QueryExecutionRequest
            {
                ParentContainerName = "orders",
                ParentQuery = "SELECT c.id FROM c",
                SecondContainerName = "items",
                SecondQuery = "SELECT c.id FROM c WHERE c.orderId = @id",
                GroupByProperty = null  // ← the problematic missing value
            },
            DatabaseName = "testdb",
            StorageFactory = Substitute.For<IStorageWriterFactory>(),
            ProgressFactory = Substitute.For<IProgressReporterFactory>()
        };

        // Act & Assert
        // Before CR-04 fix: NullReferenceException deep inside DuplicateDetector.
        // After CR-04 fix:  InvalidOperationException at the top of ExecuteAsync.
        InvalidOperationException ex =
            await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(context));

        Assert.Contains(nameof(QueryExecutionRequest.GroupByProperty), ex.Message);
        Assert.Contains(nameof(TwoLevelDuplicateFinderStrategy), ex.Message);
    }

    [Fact]
    public async Task TwoLevelDuplicateFinderStrategy_EmptyGroupByProperty_ThrowsInvalidOperationException()
    {
        // Whitespace-only is equally unusable as null
        IDatabaseClient dbClient = Substitute.For<IDatabaseClient>();
        IApplicationLogger logger = Substitute.For<IApplicationLogger>();
        SqlQueryParser parser = new();

        TwoLevelDuplicateFinderStrategy strategy = new(dbClient, logger, parser);

        StrategyExecutionContext context = new()
        {
            Request = new QueryExecutionRequest
            {
                ParentContainerName = "orders",
                ParentQuery = "SELECT c.id FROM c",
                SecondContainerName = "items",
                SecondQuery = "SELECT c.id FROM c WHERE c.orderId = @id",
                GroupByProperty = "   "  // whitespace-only
            },
            DatabaseName = "testdb",
            StorageFactory = Substitute.For<IStorageWriterFactory>(),
            ProgressFactory = Substitute.For<IProgressReporterFactory>()
        };

        await Assert.ThrowsAsync<InvalidOperationException>(() => strategy.ExecuteAsync(context));
    }

    // -----------------------------------------------------------------------
    // CR-03: CosmosDbRepository.QueryPagedAsync — paging accumulation bug
    //        (documented here; fix tracked separately)
    //
    // The bug in QueryPagedAsync (Infrastructure layer):
    //
    //   while (keepFetching)
    //   {
    //       FeedResponse<T> response = await iterator.ReadNextAsync();
    //       results.AddRange(response);          // results grows cumulatively
    //       nextToken = response.ContinuationToken;
    //       ...
    //       else if (results.Count < remaining)
    //       {
    //           remaining -= results.Count;      // ← BUG: subtracts total, not this batch
    //       }
    //   }
    //
    // Example with maxItemCount = 5:
    //   Batch 1: response has 3 items → results.Count = 3 → remaining = 5 - 3 = 2  (correct)
    //   Batch 2: response has 2 items → results.Count = 5 → remaining = 2 - 5 = -3 (WRONG)
    //
    // Correct fix: capture `int fetchedCount = response.Count` before AddRange,
    //              then use `remaining -= fetchedCount`.
    //
    // Note: This cannot be unit-tested here without mocking the Cosmos SDK Container
    // (abstract class), which would require a dedicated integration test or a thin
    // abstraction over FeedIterator. The bug is confirmed by code inspection above.
    // -----------------------------------------------------------------------
}
