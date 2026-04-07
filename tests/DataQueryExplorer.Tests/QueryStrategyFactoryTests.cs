using DataQueryExplorer.Application.Factories;
using DataQueryExplorer.Application.Services;
using DataQueryExplorer.Application.Strategies;
using DataQueryExplorer.Domain.Enums;
using DataQueryExplorer.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace DataQueryExplorer.Tests;

/// <summary>
/// Tests for <see cref="QueryStrategyFactory"/>.
///
/// Coverage gap: no tests existed for the factory's enum-to-strategy resolution,
/// including the guard against unknown/future enum values.
/// </summary>
public sealed class QueryStrategyFactoryTests
{
    /// <summary>
    /// Each valid enum value must resolve to a non-null strategy without throwing.
    /// </summary>
    [Theory]
    [InlineData(QueryStrategyType.SingleContainerQuery)]
    [InlineData(QueryStrategyType.TwoLevelJoinAllResults)]
    [InlineData(QueryStrategyType.TwoLevelJoinOrphansOnly)]
    [InlineData(QueryStrategyType.TwoLevelJoinFindDuplicates)]
    [InlineData(QueryStrategyType.ThreeLevelJoinAllResults)]
    [InlineData(QueryStrategyType.ThreeLevelJoinInnerMatchOnly)]
    public void Create_ValidStrategyType_ReturnsNonNullStrategy(QueryStrategyType type)
    {
        QueryStrategyFactory factory = BuildFactory();

        IQueryStrategy strategy = factory.Create(type);

        Assert.NotNull(strategy);
        strategy.Dispose();
    }

    /// <summary>
    /// An unrecognised enum value (e.g. a future addition or a cast of an invalid int)
    /// must throw <see cref="ArgumentOutOfRangeException"/> so callers fail fast with a
    /// clear message rather than returning null or silently running the wrong strategy.
    /// </summary>
    [Fact]
    public void Create_UnknownStrategyType_ThrowsArgumentOutOfRangeException()
    {
        QueryStrategyFactory factory = BuildFactory();

        // Cast 999 to the enum — simulates a stale/invalid enum value reaching the factory
        QueryStrategyType unknownType = (QueryStrategyType)999;

        Assert.Throws<ArgumentOutOfRangeException>(() => factory.Create(unknownType));
    }

    // -----------------------------------------------------------------------
    // Helpers
    // -----------------------------------------------------------------------

    private static QueryStrategyFactory BuildFactory()
    {
        // Register only real strategy types; all other singletons are mocked so
        // nothing tries to hit a real database or file system.
        ServiceCollection services = new();
        services.AddSingleton(Substitute.For<IDatabaseClient>());
        services.AddSingleton(Substitute.For<IApplicationLogger>());
        services.AddSingleton<SqlQueryParser>();

        services.AddTransient<SingleContainerQueryStrategy>();
        services.AddTransient<TwoLevelJoinStrategy>();
        services.AddTransient<TwoLevelOrphanFinderStrategy>();
        services.AddTransient<TwoLevelDuplicateFinderStrategy>();
        services.AddTransient<ThreeLevelJoinStrategy>();
        services.AddTransient<ThreeLevelInnerJoinStrategy>();

        services.AddSingleton<QueryStrategyFactory>();

        ServiceProvider provider = services.BuildServiceProvider();
        return provider.GetRequiredService<QueryStrategyFactory>();
    }
}
