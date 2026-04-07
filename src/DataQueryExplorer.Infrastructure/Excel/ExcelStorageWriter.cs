namespace DataQueryExplorer.Infrastructure.Excel;

/// <summary>
/// ClosedXML-backed implementation of <see cref="IStorageWriter"/> for a single worksheet.
/// Tracks the current row internally; headers always occupy row 1.
/// </summary>
internal sealed class ExcelStorageWriter : IStorageWriter
{
    private readonly IXLWorksheet _worksheet;
    private int _nextRow = 1;

    internal ExcelStorageWriter(IXLWorksheet worksheet)
    {
        _worksheet = worksheet;
    }

    public int NextRow => _nextRow;

    public void WriteHeaders(IEnumerable<string> headers)
    {
        int col = 1;
        foreach (string header in headers)
        {
            IXLCell cell = _worksheet.Cell(_nextRow, col++);
            cell.Value = header ?? string.Empty;
            cell.Style.Font.Bold = true;
        }
        _nextRow++;
    }

    public int WriteRow(IEnumerable<string?> values)
    {
        int col = 1;
        int writtenRow = _nextRow;
        foreach (string? value in values)
            _worksheet.Cell(_nextRow, col++).Value = value ?? string.Empty;
        _nextRow++;
        return writtenRow;
    }

    public void WriteCell(string? value, int row, int column)
        => _worksheet.Cell(row, column).Value = value ?? string.Empty;
}
