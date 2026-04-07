namespace DataQueryExplorer.Infrastructure.Excel;

/// <summary>
/// Creates named worksheets inside a single <see cref="XLWorkbook"/> and saves them all together.
/// Register as Transient — one factory (one workbook) per execution run.
/// </summary>
public sealed class ExcelStorageWriterFactory : IStorageWriterFactory
{
    private readonly XLWorkbook _workbook = new();

    public IStorageWriter CreateWriter(string sheetName)
    {
        IXLWorksheet sheet = _workbook.Worksheets.Add(sheetName);
        return new ExcelStorageWriter(sheet);
    }

    public Task SaveAsync(string filePath)
    {
        _workbook.SaveAs(filePath);
        return Task.CompletedTask;
    }

    public void Dispose() => _workbook.Dispose();
}
