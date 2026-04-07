namespace DataQueryExplorer.Domain.Interfaces;

public interface IDatabaseRepository<T> : IDisposable where T : class
{
    Task<IEnumerable<T>> QueryAsync(string query, IDictionary<string, object>? parameters = null);

    Task<PagedResult<T>> QueryPagedAsync(
        string query,
        string? continuationToken,
        int maxItemCount,
        IDictionary<string, object>? parameters = null);
}
