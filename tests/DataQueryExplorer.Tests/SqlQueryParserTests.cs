using DataQueryExplorer.Application.Services;
using Xunit;

namespace DataQueryExplorer.Tests;

public sealed class SqlQueryParserTests
{
    private readonly SqlQueryParser _parser = new();

    // ------------------------------------------------------------------
    // ExtractColumnHeaders
    // ------------------------------------------------------------------

    [Fact]
    public void ExtractColumnHeaders_SimpleAliased_ReturnsLastSegments()
    {
        var headers = _parser.ExtractColumnHeaders("SELECT c.id, c.name, c.email FROM c");
        Assert.Equal(new[] { "id", "name", "email" }, headers);
    }

    [Fact]
    public void ExtractColumnHeaders_NoAlias_ReturnsSingleSegment()
    {
        var headers = _parser.ExtractColumnHeaders("SELECT id, name FROM c");
        Assert.Equal(new[] { "id", "name" }, headers);
    }

    [Fact]
    public void ExtractColumnHeaders_CaseInsensitiveKeywords_Works()
    {
        var headers = _parser.ExtractColumnHeaders("select c.id from c");
        Assert.Single(headers);
        Assert.Equal("id", headers[0]);
    }

    [Fact]
    public void ExtractColumnHeaders_MissingSelect_ReturnsEmpty()
    {
        var headers = _parser.ExtractColumnHeaders("FROM c WHERE c.id = 1");
        Assert.Empty(headers);
    }

    // ------------------------------------------------------------------
    // ExtractParameters
    // ------------------------------------------------------------------

    [Fact]
    public void ExtractParameters_SingleParam_ReturnsParamName()
    {
        var result = _parser.ExtractParameters("SELECT c.id FROM c WHERE c.type = @type");
        Assert.Single(result);
        Assert.Equal("type", result[0]);
    }

    [Fact]
    public void ExtractParameters_MultipleParams_ReturnsAll()
    {
        var result = _parser.ExtractParameters(
            "SELECT c.id FROM c WHERE c.type = @type AND c.code = @code");
        Assert.Equal(2, result.Count);
        Assert.Contains("type", result);
        Assert.Contains("code", result);
    }

    [Fact]
    public void ExtractParameters_EmailAddress_IsIgnored()
    {
        // The @ in an email inside a string literal should not be treated as a SQL param
        var result = _parser.ExtractParameters(
            "SELECT c.id FROM c WHERE c.email = 'user@example.com'");
        Assert.Empty(result);
    }

    [Fact]
    public void ExtractParameters_NoDuplicates_WhenSameParamAppearsTwice()
    {
        var result = _parser.ExtractParameters(
            "SELECT c.id FROM c WHERE c.a = @id OR c.b = @id");
        Assert.Single(result);
        Assert.Equal("id", result[0]);
    }

    [Fact]
    public void ExtractParameters_EmptyQuery_ReturnsEmpty()
    {
        Assert.Empty(_parser.ExtractParameters(string.Empty));
        Assert.Empty(_parser.ExtractParameters("   "));
    }

    // ------------------------------------------------------------------
    // HasParameters
    // ------------------------------------------------------------------

    [Fact]
    public void HasParameters_WithParam_ReturnsTrue()
        => Assert.True(_parser.HasParameters("SELECT * FROM c WHERE c.id = @id"));

    [Fact]
    public void HasParameters_WithoutParam_ReturnsFalse()
        => Assert.False(_parser.HasParameters("SELECT * FROM c"));

    // ------------------------------------------------------------------
    // BuildCountQuery
    // ------------------------------------------------------------------

    [Fact]
    public void BuildCountQuery_WrapsQueryCorrectly()
    {
        var count = _parser.BuildCountQuery("SELECT c.id FROM c WHERE c.type = 'order'");
        Assert.StartsWith("SELECT count(0) as CountOfResult FROM", count);
        Assert.Contains("c.type = 'order'", count);
    }

    [Fact]
    public void BuildCountQuery_CaseInsensitiveFrom()
    {
        var count = _parser.BuildCountQuery("select c.id from c");
        Assert.StartsWith("SELECT count(0) as CountOfResult FROM", count, StringComparison.OrdinalIgnoreCase);
    }
}
