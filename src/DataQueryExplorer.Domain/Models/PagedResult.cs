namespace DataQueryExplorer.Domain;

public sealed class PagedResult<T> : IDisposable where T : class
{
    public IEnumerable<T> Items { get; }
    public string? ContinuationToken { get; }

    public PagedResult(IEnumerable<T> items, string? continuationToken)
    {
        Items = items;
        ContinuationToken = continuationToken;
    }

    public void Dispose() => GC.SuppressFinalize(this);
}
