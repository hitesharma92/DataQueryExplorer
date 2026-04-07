namespace DataQueryExplorer.Domain;

/// <summary>
/// Carries all inputs needed for a single query execution run.
/// The Console layer builds this before handing off to a strategy.
/// </summary>
public sealed class QueryExecutionRequest
{
    public required string ParentContainerName { get; init; }
    public required string ParentQuery { get; init; }

    // Two-level / three-level
    public string? SecondContainerName { get; init; }
    public string? SecondQuery { get; init; }

    // Three-level only
    public string? ThirdContainerName { get; init; }
    public string? ThirdQuery { get; init; }

    // Duplicate finder
    public string? GroupByProperty { get; init; }
    public int GroupByPropertyThreshold { get; init; }

    // Pre-read rows from an input Excel file for parameterised queries
    public IReadOnlyList<IReadOnlyDictionary<string, string>>? ParameterRows { get; init; }
}
