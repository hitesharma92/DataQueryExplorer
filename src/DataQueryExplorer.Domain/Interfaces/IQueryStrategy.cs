namespace DataQueryExplorer.Domain.Interfaces;

public interface IQueryStrategy : IDisposable
{
    Task ExecuteAsync(StrategyExecutionContext context);
}
