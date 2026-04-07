namespace DataQueryExplorer.Domain.Interfaces;

public interface IStorageWriterFactory : IDisposable
{
    IStorageWriter CreateWriter(string sheetName);
    Task SaveAsync(string filePath);
}
