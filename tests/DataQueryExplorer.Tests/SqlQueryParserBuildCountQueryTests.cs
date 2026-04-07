using DataQueryExplorer.Application.Services;
using DataQueryExplorer.Domain.Constants;

namespace DataQueryExplorer.Tests;

/// <summary>
/// Tests for <see cref="SqlQueryParser.BuildCountQuery"/>.
/// These extend the existing SqlQueryParserTests to cover a method that had no tests.
/// </summary>
public sealed class SqlQueryParserBuildCountQueryTests
{
    private readonly SqlQueryParser _parser = new();

    [Fact]
    public void BuildCountQuery_SimpleQuery_InjectsCOUNTAndPreservesContainerAlias()
    {
        string query = "SELECT c.id, c.name FROM c";

        string result = _parser.BuildCountQuery(query);

        Assert.Contains($"SELECT count(0) as {AppConstants.CountColumnName}", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("FROM c", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCountQuery_QueryWithWhereClause_PreservesConditions()
    {
        string query = "SELECT c.id FROM c WHERE c.type = 'order' AND c.status = 'active'";

        string result = _parser.BuildCountQuery(query);

        // The condition after FROM must survive unchanged
        Assert.Contains("WHERE c.type = 'order' AND c.status = 'active'", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("count(0)", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCountQuery_QueryWithNoFROM_ReturnsOriginalQueryUnchanged()
    {
        // A malformed query with no FROM keyword should fall through safely
        string query = "SELECT c.id";

        string result = _parser.BuildCountQuery(query);

        Assert.Equal(query, result);
    }

    [Fact]
    public void BuildCountQuery_CaseInsensitiveFROM_StillWorks()
    {
        string query = "select c.id from c where c.active = true";

        string result = _parser.BuildCountQuery(query);

        Assert.Contains("count(0)", result, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("where c.active = true", result, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildCountQuery_ResultColumn_MatchesAppConstantsCountColumnName()
    {
        // Verifies the constant name used in BuildCountQuery matches what
        // GetCountAsync reads back via JObject.Value<int>(AppConstants.CountColumnName).
        string query = "SELECT c.id FROM c";

        string result = _parser.BuildCountQuery(query);

        // The produced query must reference exactly the constant name so that
        // CosmosDbRepository.QueryAsync → GetCountAsync can deserialise the field.
        Assert.Contains(AppConstants.CountColumnName, result, StringComparison.Ordinal);
    }
}
