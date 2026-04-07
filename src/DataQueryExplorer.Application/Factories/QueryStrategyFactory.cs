using DataQueryExplorer.Application.Strategies;
using DataQueryExplorer.Domain.Enums;
using DataQueryExplorer.Domain.Interfaces;
using Microsoft.Extensions.DependencyInjection;

namespace DataQueryExplorer.Application.Factories;

/// <summary>
/// Resolves the correct <see cref="IQueryStrategy"/> for a given <see cref="QueryStrategyType"/>.
/// Uses the DI container to create strategy instances, keeping them Transient
/// (each instance owns its own repository references).
/// </summary>
public sealed class QueryStrategyFactory
{
    private readonly IServiceProvider _serviceProvider;

    public QueryStrategyFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public IQueryStrategy Create(QueryStrategyType type) => type switch
    {
        QueryStrategyType.SingleContainerQuery => _serviceProvider.GetRequiredService<SingleContainerQueryStrategy>(),
        QueryStrategyType.TwoLevelJoinAllResults => _serviceProvider.GetRequiredService<TwoLevelJoinStrategy>(),
        QueryStrategyType.TwoLevelJoinOrphansOnly => _serviceProvider.GetRequiredService<TwoLevelOrphanFinderStrategy>(),
        QueryStrategyType.TwoLevelJoinFindDuplicates => _serviceProvider.GetRequiredService<TwoLevelDuplicateFinderStrategy>(),
        QueryStrategyType.ThreeLevelJoinAllResults => _serviceProvider.GetRequiredService<ThreeLevelJoinStrategy>(),
        QueryStrategyType.ThreeLevelJoinInnerMatchOnly => _serviceProvider.GetRequiredService<ThreeLevelInnerJoinStrategy>(),
        _ => throw new ArgumentOutOfRangeException(nameof(type), $"Unknown strategy type: {type}")
    };
}
