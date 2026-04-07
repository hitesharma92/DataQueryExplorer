using DataQueryExplorer.Console.Logging;

namespace DataQueryExplorer.Tests;

/// <summary>
/// Tests for <see cref="ConsoleApplicationLogger"/> disposal safety.
///
/// CR-08 context: <c>CloseLog()</c> is not idempotent — it does not check <c>_disposed</c>.
/// In <c>Program.cs</c>, <c>logger.CloseLog()</c> is called in the <c>finally</c> block,
/// then the DI container's <c>await using (provider)</c> calls <c>Dispose()</c> a second time,
/// which in turn calls <c>CloseLog()</c> again on an already-closed <c>StreamWriter</c>.
/// The <c>WriteToFile</c> path is protected by a try/catch, but <c>CloseLog</c> calls
/// <c>_writer?.Flush()</c> and <c>_writer?.Close()</c> directly without that guard.
///
/// Fix for CR-08 is tracked separately. These tests document the expected safe behaviour
/// once the fix is applied, and will catch regressions.
/// </summary>
public sealed class ConsoleApplicationLoggerTests : IDisposable
{
    // Each test gets its own clearly named log so parallel runs don't collide
    private readonly string _logName;

    public ConsoleApplicationLoggerTests()
    {
        _logName = $"Test_{Guid.NewGuid():N}";
    }

    public void Dispose()
    {
        // Clean up log directories created during tests
        string dir = $"./Logs - {_logName}";
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);
    }

    [Fact]
    public void OpenLog_ThenDispose_NoException()
    {
        ConsoleApplicationLogger logger = new();
        logger.OpenLog(_logName);

        // Should not throw
        logger.Dispose();
    }

    [Fact]
    public void Dispose_CalledTwice_NoException()
    {
        ConsoleApplicationLogger logger = new();
        logger.OpenLog(_logName);
        logger.Dispose();

        // Second dispose must be a no-op — IDisposable contract
        Exception? caught = Record.Exception(() => logger.Dispose());

        Assert.Null(caught);
    }

    [Fact]
    public void LogToConsole_AfterDispose_DoesNotThrow()
    {
        // WriteToFile is guarded by _disposed check, so logging after dispose
        // should be silently ignored rather than crashing.
        ConsoleApplicationLogger logger = new();
        logger.OpenLog(_logName);
        logger.Dispose();

        Exception? caught = Record.Exception(() => logger.LogToConsole("message after dispose"));

        Assert.Null(caught);
    }

    [Fact]
    public void CloseLog_AfterDispose_DoesNotThrow()
    {
        // Reproduces the CR-08 double-call scenario:
        // Program.cs: finally { logger.CloseLog(); }  ← first call
        // DI container DisposeAsync: logger.Dispose() → CloseLog() ← second call
        //
        // Without the fix (adding `if (_disposed) return` to CloseLog), the second call
        // reaches _writer?.Flush() on a closed StreamWriter and throws ObjectDisposedException.
        ConsoleApplicationLogger logger = new();
        logger.OpenLog(_logName);

        logger.CloseLog(); // simulates Program.cs finally block
        logger.Dispose();  // simulates DI container disposal → calls CloseLog() internally

        // If CR-08 is NOT fixed this test will throw ObjectDisposedException.
        // Once fixed it should pass cleanly.
    }

    [Fact]
    public void LogError_WithException_DoesNotThrow()
    {
        ConsoleApplicationLogger logger = new();
        logger.OpenLog(_logName);

        Exception? caught = Record.Exception(() =>
            logger.LogError("test error", new InvalidOperationException("inner")));

        logger.Dispose();
        Assert.Null(caught);
    }

    [Fact]
    public void OpenLog_CreatesLogFile()
    {
        ConsoleApplicationLogger logger = new();
        logger.OpenLog(_logName);

        string dir = $"./Logs - {_logName}";
        bool logFileExists = Directory.Exists(dir) && Directory.GetFiles(dir, "*.txt").Length > 0;

        logger.Dispose();
        Assert.True(logFileExists);
    }
}
