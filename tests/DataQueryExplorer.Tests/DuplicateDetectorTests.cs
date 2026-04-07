using DataQueryExplorer.Application.Utilities;
using Newtonsoft.Json.Linq;
using Xunit;

namespace DataQueryExplorer.Tests;

public sealed class DuplicateDetectorTests
{
    private static JObject MakeDoc(string id) =>
        JObject.Parse($@"{{""id"":""{id}"",""type"":""record""}}");

    // ------------------------------------------------------------------
    // GroupAndCount
    // ------------------------------------------------------------------

    [Fact]
    public void GroupAndCount_WithDuplicates_ReturnsCorrectCounts()
    {
        var docs = new[] { MakeDoc("A"), MakeDoc("A"), MakeDoc("B") };
        var result = DuplicateDetector.GroupAndCount(docs, "id");

        Assert.Equal(2, result["A"]);
        Assert.Equal(1, result["B"]);
    }

    [Fact]
    public void GroupAndCount_EmptyList_ReturnsEmptyDictionary()
    {
        var result = DuplicateDetector.GroupAndCount(Enumerable.Empty<JObject>(), "id");
        Assert.Empty(result);
    }

    [Fact]
    public void GroupAndCount_MissingProperty_DocumentsExcluded()
    {
        var docs = new[]
        {
            JObject.Parse(@"{""name"":""Alice""}"),
            JObject.Parse(@"{""id"":""X""}"),
        };
        var result = DuplicateDetector.GroupAndCount(docs, "id");
        Assert.Single(result);
        Assert.Equal(1, result["X"]);
    }

    // ------------------------------------------------------------------
    // GetDuplicatesAboveThreshold
    // ------------------------------------------------------------------

    [Fact]
    public void GetDuplicatesAboveThreshold_ReturnsOnlyAbove()
    {
        var docs = new[]
        {
            MakeDoc("A"), MakeDoc("A"), MakeDoc("A"), // count 3
            MakeDoc("B"), MakeDoc("B"),               // count 2
            MakeDoc("C"),                             // count 1
        };

        var result = DuplicateDetector.GetDuplicatesAboveThreshold(docs, "id", threshold: 2);

        Assert.Single(result);
        Assert.True(result.ContainsKey("A"));
    }

    [Fact]
    public void GetDuplicatesAboveThreshold_Threshold0_ReturnsAllWithMoreThanOne()
    {
        var docs = new[] { MakeDoc("A"), MakeDoc("A"), MakeDoc("B") };
        var result = DuplicateDetector.GetDuplicatesAboveThreshold(docs, "id", threshold: 0);

        Assert.Equal(2, result.Count); // A=2, B=1 — both >0
    }

    [Fact]
    public void GetDuplicatesAboveThreshold_NoDuplicates_ReturnsEmpty()
    {
        var docs = new[] { MakeDoc("A"), MakeDoc("B"), MakeDoc("C") };
        var result = DuplicateDetector.GetDuplicatesAboveThreshold(docs, "id", threshold: 1);
        Assert.Empty(result);
    }
}
