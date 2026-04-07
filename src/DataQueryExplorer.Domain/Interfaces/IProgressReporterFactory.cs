namespace DataQueryExplorer.Domain.Interfaces;

public interface IProgressReporterFactory
{
    IProgressReporter Create(int totalCount, string label = "Processing...");
}
