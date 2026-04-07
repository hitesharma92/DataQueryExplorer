using DataQueryExplorer.Domain.Interfaces;
using ShellProgressBar;

namespace DataQueryExplorer.Console.UI;

internal sealed class ConsoleProgressReporter : IProgressReporter
{
    private readonly ProgressBar _bar;
    internal ConsoleProgressReporter(ProgressBar bar) => _bar = bar;
    public void Tick() => _bar.Tick();
    public void Dispose() => _bar.Dispose();
}

/// <summary>
/// Creates <see cref="IProgressReporter"/> instances backed by <see cref="ShellProgressBar"/>.
/// </summary>
public sealed class ConsoleProgressReporterFactory : IProgressReporterFactory
{
    public IProgressReporter Create(int totalCount, string label = "Processing...")
    {
        ProgressBarOptions options = new ProgressBarOptions
        {
            ForegroundColor = ConsoleColor.Yellow,
            ForegroundColorDone = ConsoleColor.DarkGreen,
            BackgroundColor = ConsoleColor.DarkGray,
            BackgroundCharacter = '\u2593'
        };
        return new ConsoleProgressReporter(new ProgressBar(totalCount, label, options));
    }
}
