namespace DataQueryExplorer.Infrastructure.Excel;

/// <summary>
/// Reads parameter values from an Excel file provided by the user.
/// Used when a query contains SQL parameters (@paramName) that must be resolved row-by-row.
/// </summary>
public sealed class InputExcelReader
{
    /// <summary>
    /// Reads all data rows from the first worksheet of the given Excel file.
    /// The first row is treated as headers. Each subsequent row becomes a dictionary
    /// mapping header name → cell value.
    /// </summary>
    /// <param name="filePath">Absolute path to the .xlsx file.</param>
    /// <returns>Row data as a list of header→value dictionaries.</returns>
    public IReadOnlyList<IReadOnlyDictionary<string, string>> ReadRows(string filePath)
    {
        XLWorkbook workbook = new(filePath);
        using (workbook)
        {
            IXLWorksheet sheet = workbook.Worksheet(1);
            List<IXLRangeRow>? rows = sheet.RangeUsed()?.RowsUsed().ToList();

            if (rows is null || rows.Count < 2)
                return Array.Empty<IReadOnlyDictionary<string, string>>();

            List<string> headers = rows[0].Cells().Select(c => c.GetString().Trim()).ToList();
            List<IReadOnlyDictionary<string, string>> result = new();

            for (int i = 1; i < rows.Count; i++)
            {
                List<IXLCell> cells = rows[i].Cells().ToList();
                Dictionary<string, string> row = new(StringComparer.OrdinalIgnoreCase);
                for (int j = 0; j < headers.Count; j++)
                {
                    string value = j < cells.Count ? cells[j].GetString().Trim() : string.Empty;
                    row[headers[j]] = value;
                }
                result.Add(row);
            }

            return result;
        }
    }

    /// <summary>
    /// Returns the column header names (first row) from the given Excel file.
    /// </summary>
    public IReadOnlyList<string> ReadHeaders(string filePath)
    {
        XLWorkbook workbook = new(filePath);
        using (workbook)
        {
            IXLWorksheet sheet = workbook.Worksheet(1);
            IXLRangeRow? firstRow = sheet.RangeUsed()?.FirstRow();
            return firstRow?.Cells().Select(c => c.GetString().Trim()).ToList()
                   ?? (IReadOnlyList<string>)Array.Empty<string>();
        }
    }
}
