namespace DataQueryExplorer.Domain.Interfaces;

public interface IStorageWriter
{
    void WriteHeaders(IEnumerable<string> headers);

    /// <summary>Writes a data row and returns the 1-based row number where data was written.</summary>
    int WriteRow(IEnumerable<string?> values);

    void WriteCell(string? value, int row, int column);

    int NextRow { get; }
}
