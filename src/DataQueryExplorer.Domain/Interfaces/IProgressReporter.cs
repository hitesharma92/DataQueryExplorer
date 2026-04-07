namespace DataQueryExplorer.Domain.Interfaces;

public interface IProgressReporter : IDisposable
{
    void Tick();
}
