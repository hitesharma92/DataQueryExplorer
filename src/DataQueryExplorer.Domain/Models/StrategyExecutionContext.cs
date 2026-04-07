using DataQueryExplorer.Domain.Interfaces;

namespace DataQueryExplorer.Domain;

/// <summary>
/// Bundles all per-execution dependencies that are passed into <see cref="IQueryStrategy.ExecuteAsync"/>.
/// </summary>
public sealed class StrategyExecutionContext
{
    public required QueryExecutionRequest Request { get; init; }
    public required string DatabaseName { get; init; }
    public required IStorageWriterFactory StorageFactory { get; init; }
    public required IProgressReporterFactory ProgressFactory { get; init; }
}
