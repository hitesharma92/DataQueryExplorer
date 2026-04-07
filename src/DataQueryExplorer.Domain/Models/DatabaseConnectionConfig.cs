namespace DataQueryExplorer.Domain;

public sealed class DatabaseConnectionConfig
{
    public required string Endpoint { get; init; }
    public required string Key { get; init; }
}
