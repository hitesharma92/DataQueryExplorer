using Newtonsoft.Json.Linq;

namespace DataQueryExplorer.Application.Utilities;

/// <summary>
/// Groups a collection of <see cref="JObject"/> documents by a specified property
/// and counts occurrences. Used to detect duplicate values across child documents.
/// </summary>
public static class DuplicateDetector
{
    /// <summary>
    /// Groups documents by the given property value and returns a dictionary
    /// mapping property value → occurrence count.
    /// Documents that do not contain the property are excluded.
    /// </summary>
    public static Dictionary<string, int> GroupAndCount(
        IEnumerable<JObject> documents,
        string propertyName)
    {
        return documents
            .Where(d => d.Property(propertyName) is not null)
            .GroupBy(d => d[propertyName]!.ToString())
            .ToDictionary(g => g.Key, g => g.Count());
    }

    /// <summary>
    /// Returns only the entries whose count exceeds the given threshold.
    /// </summary>
    public static Dictionary<string, int> GetDuplicatesAboveThreshold(
        IEnumerable<JObject> documents,
        string propertyName,
        int threshold)
    {
        return GroupAndCount(documents, propertyName)
            .Where(kv => kv.Value > threshold)
            .ToDictionary(kv => kv.Key, kv => kv.Value);
    }
}
