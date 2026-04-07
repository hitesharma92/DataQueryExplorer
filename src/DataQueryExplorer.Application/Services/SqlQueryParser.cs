namespace DataQueryExplorer.Application.Services;

/// <summary>
/// Parses and transforms SQL query strings.
/// Registered as a singleton — it is stateless.
/// </summary>
public sealed class SqlQueryParser
{
    private const string SqlParamPattern = @"(?<![a-zA-Z0-9.%])@[a-zA-Z_][a-zA-Z0-9_]*";

    /// <summary>
    /// Extracts column display names from the SELECT … FROM clause.
    /// For "SELECT c.id, c.name FROM …" returns ["id", "name"].
    /// Handles aliases like "c.field" — takes only the last segment after a dot or space.
    /// </summary>
    public string[] ExtractColumnHeaders(string query)
    {
        int selectEnd = query.IndexOf("SELECT", StringComparison.OrdinalIgnoreCase);
        if (selectEnd < 0) return Array.Empty<string>();
        selectEnd += "SELECT".Length;

        int fromStart = query.LastIndexOf("FROM", StringComparison.OrdinalIgnoreCase);
        if (fromStart < 0 || fromStart <= selectEnd) return Array.Empty<string>();

        string selectClause = query[selectEnd..fromStart].Trim();
        string[] parts = selectClause.Split(',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);

        return parts.Select(ExtractLastSegment).ToArray();
    }

    private static string ExtractLastSegment(string expression)
    {
        List<char> chars = new(expression.Length);
        foreach (char c in expression)
        {
            if (c == ' ' || c == '.')
                chars.Clear();
            else
                chars.Add(c);
        }
        return new string(chars.ToArray());
    }

    /// <summary>
    /// Returns the @param names used in the query, excluding those that appear
    /// to be email addresses or inside string literals.
    /// </summary>
    public IReadOnlyList<string> ExtractParameters(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return Array.Empty<string>();

        List<string> results = new();
        try
        {
            MatchCollection matches = Regex.Matches(query, SqlParamPattern, RegexOptions.None, TimeSpan.FromSeconds(1));
            foreach (Match match in matches)
            {
                if (IsOutsideStringLiteral(query, match.Index))
                {
                    string name = match.Value[1..]; // strip '@'
                    if (!results.Contains(name, StringComparer.OrdinalIgnoreCase))
                        results.Add(name);
                }
            }
        }
        catch (RegexMatchTimeoutException)
        {
            // Fallback: conservatively return nothing to avoid false positives
        }
        return results;
    }

    /// <summary>Returns true when the query has at least one SQL parameter.</summary>
    public bool HasParameters(string query) => ExtractParameters(query).Count > 0;

    /// <summary>
    /// Wraps an arbitrary query as a count query:
    /// "SELECT count(0) as CountOfResult FROM …[same conditions]"
    /// </summary>
    public string BuildCountQuery(string query)
    {
        string[] parts = Regex.Split(query, @"\bFROM\b", RegexOptions.IgnoreCase);
        if (parts.Length < 2) return query;
        return $"SELECT count(0) as {AppConstants.CountColumnName} FROM{parts[1]}";
    }

    private static bool IsOutsideStringLiteral(string query, int position)
    {
        int singleQuotes = 0;
        for (int i = 0; i < position; i++)
            if (query[i] == '\'') singleQuotes++;
        return singleQuotes % 2 == 0;
    }
}
