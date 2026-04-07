namespace DataQueryExplorer.Domain.Interfaces;

public interface IApplicationLogger
{
    void LogInfo(string message);
    void LogToConsole(string message);
    void LogError(string message, Exception? exception = null);
    void OpenLog(string logName);
    void CloseLog();
}
