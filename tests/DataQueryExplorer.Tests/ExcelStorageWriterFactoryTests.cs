using ClosedXML.Excel;
using DataQueryExplorer.Infrastructure.Excel;
using Xunit;

namespace DataQueryExplorer.Tests;

public sealed class ExcelStorageWriterFactoryTests : IDisposable
{
    private readonly string _tempFile = Path.Combine(Path.GetTempPath(),
        $"DataQueryExplorerTest_{Guid.NewGuid()}.xlsx");

    [Fact]
    public async Task CreateWriter_WritesHeadersAndRows_ThenSavesReadableFile()
    {
        using var factory = new ExcelStorageWriterFactory();
        var writer = factory.CreateWriter("TestSheet");

        writer.WriteHeaders(new[] { "Id", "Name", "Status" });
        writer.WriteRow(new[] { "1", "Alice", "Active" });
        writer.WriteRow(new[] { "2", "Bob", "Inactive" });

        await factory.SaveAsync(_tempFile);

        // Verify by reading back with ClosedXML
        using var wb = new XLWorkbook(_tempFile);
        var sheet = wb.Worksheet("TestSheet");

        Assert.Equal("Id", sheet.Cell(1, 1).GetString());
        Assert.Equal("Name", sheet.Cell(1, 2).GetString());
        Assert.Equal("Status", sheet.Cell(1, 3).GetString());
        Assert.True(sheet.Cell(1, 1).Style.Font.Bold);

        Assert.Equal("1", sheet.Cell(2, 1).GetString());
        Assert.Equal("Alice", sheet.Cell(2, 2).GetString());
        Assert.Equal("Active", sheet.Cell(2, 3).GetString());

        Assert.Equal("2", sheet.Cell(3, 1).GetString());
        Assert.Equal("Bob", sheet.Cell(3, 2).GetString());
    }

    [Fact]
    public async Task WriteCell_WritesAtCorrectPosition()
    {
        using var factory = new ExcelStorageWriterFactory();
        var writer = factory.CreateWriter("CellTest");

        writer.WriteHeaders(new[] { "Col1", "Col2" });
        int rowWritten = writer.WriteRow(new[] { "A", "B" });

        // Write a value in the same row but next column
        writer.WriteCell("extra", rowWritten, 3);

        await factory.SaveAsync(_tempFile);

        using var wb = new XLWorkbook(_tempFile);
        var sheet = wb.Worksheet("CellTest");
        Assert.Equal("extra", sheet.Cell(rowWritten, 3).GetString());
    }

    [Fact]
    public async Task CreateWriter_MultipleSheets_AllPresentInWorkbook()
    {
        using var factory = new ExcelStorageWriterFactory();
        factory.CreateWriter("Sheet1").WriteHeaders(new[] { "A" });
        factory.CreateWriter("Sheet2").WriteHeaders(new[] { "B" });

        await factory.SaveAsync(_tempFile);

        using var wb = new XLWorkbook(_tempFile);
        Assert.Equal(2, wb.Worksheets.Count);
        Assert.NotNull(wb.Worksheet("Sheet1"));
        Assert.NotNull(wb.Worksheet("Sheet2"));
    }

    [Fact]
    public void WriteRow_ReturnsCorrectRowNumbers()
    {
        using var factory = new ExcelStorageWriterFactory();
        var writer = factory.CreateWriter("Rows");
        writer.WriteHeaders(new[] { "X" });

        int row1 = writer.WriteRow(new[] { "first" });
        int row2 = writer.WriteRow(new[] { "second" });

        Assert.Equal(2, row1); // row 1 = headers
        Assert.Equal(3, row2);
    }

    public void Dispose()
    {
        if (File.Exists(_tempFile))
            File.Delete(_tempFile);
    }
}
