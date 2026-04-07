namespace DataQueryExplorer.Console.Logging;

/// <summary>
/// Writes log entries to a timestamped file and optionally mirrors them to the console.
/// Register as Singleton in DI. Call <see cref="OpenLog"/> before use.
/// </summary>
public sealed class ConsoleApplicationLogger : IApplicationLogger, IDisposable
{
    private StreamWriter? _writer;
    private readonly object _lock = new();
    private bool _disposed;

    public void OpenLog(string logName)
    {
        string dir = $"./Logs - {logName}";
        Directory.CreateDirectory(dir);
        string fileName = Path.Combine(dir, $"Log-{DateTime.Now:yyyy-MM-dd-HH-mm-ss}-{logName}.txt");
        _writer = new StreamWriter(fileName, append: true);
        WriteToFile("=== Log Started ===");
        System.Console.WriteLine($"\nLog file: {Path.GetFullPath(fileName)}\n");
    }

    public void LogInfo(string message)
        => WriteToFile(message);

    public void LogToConsole(string message)
    {
        System.Console.WriteLine(message);
        WriteToFile(message);
    }

    public void LogError(string message, Exception? exception = null)
    {
        string full = exception is not null
            ? $"{message}{Environment.NewLine}Exception: {exception.Message}{Environment.NewLine}Stack: {exception.StackTrace}"
            : message;
        System.Console.WriteLine(full);
        WriteToFile(full);
    }

    public void CloseLog()
    {
        WriteToFile("=== Log Closed ===");
        lock (_lock)
        {
            _writer?.Flush();
            _writer?.Close();
        }
    }

    private void WriteToFile(string message)
    {
        if (_disposed || _writer is null)
            return;

        lock (_lock)
        {
            try
            {
                if (_writer is not null && _writer.BaseStream.CanWrite)
                {
                    _writer.WriteLine($"{DateTime.Now:dd/MM/yyyy HH:mm:ss}    {message}");
                    _writer.Flush();
                }
            }
            catch (ObjectDisposedException)
            {
                // Stream already disposed, silently ignore
            }
            catch (IOException)
            {
                // Stream write error, silently ignore
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;
        CloseLog();
        lock (_lock)
        {
            _writer?.Dispose();
        }
        GC.SuppressFinalize(this);
    }
}
