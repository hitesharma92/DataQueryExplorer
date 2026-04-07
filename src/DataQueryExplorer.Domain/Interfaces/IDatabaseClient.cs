namespace DataQueryExplorer.Domain.Interfaces;

public interface IDatabaseClient
{
    Task VerifyConnectionAsync(string endpoint, string key);
    Task<IReadOnlyList<string>> ListDatabasesAsync();
    Task<IReadOnlyList<string>> ListContainersAsync(string databaseName);
    IDatabaseRepository<T> GetRepository<T>(string containerName, string databaseName) where T : class;
}
