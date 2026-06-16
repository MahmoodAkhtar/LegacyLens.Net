using System.Diagnostics;

namespace LegacyLens.Cli.Progress;

public sealed class ConsoleScanProgressReporter : IScanProgressReporter
{
    private static readonly char[] SpinnerFrames = ['|', '/', '-', '\\'];

    private readonly bool _verbose;
    private readonly Stopwatch _stopwatch = new();
    private int _spinnerIndex;

    public ConsoleScanProgressReporter(bool verbose)
    {
        _verbose = verbose;
    }

    public void ScanStarted(string scanPath, string outputPath)
    {
        _stopwatch.Restart();

        Console.WriteLine("LegacyLens.NET");
        Console.WriteLine();
        Console.WriteLine($"Scan path: {scanPath}");
        Console.WriteLine($"Report: {outputPath}");
        Console.WriteLine();
        Console.WriteLine("Scanning...");
        Console.WriteLine();
    }

    public void PhaseStarted(string phaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseName);

        Console.WriteLine($"{NextSpinnerFrame()} {phaseName}...");
    }

    public void PhaseCompleted(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        Console.WriteLine($"✓ {message}");
    }

    public void VerboseDetail(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);

        if (!_verbose)
        {
            return;
        }

        Console.WriteLine($"  {message}");
    }

    public void ScanCompleted()
    {
        if (!_stopwatch.IsRunning)
        {
            return;
        }

        _stopwatch.Stop();

        Console.WriteLine();
        Console.WriteLine($"Completed in {_stopwatch.Elapsed:hh\\:mm\\:ss}");
        Console.WriteLine();
    }

    private char NextSpinnerFrame()
    {
        var frame = SpinnerFrames[_spinnerIndex % SpinnerFrames.Length];
        _spinnerIndex++;
        return frame;
    }
}
