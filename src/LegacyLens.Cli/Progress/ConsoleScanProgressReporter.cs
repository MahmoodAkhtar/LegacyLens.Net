using System.Diagnostics;

namespace LegacyLens.Cli.Progress;

public sealed class ConsoleScanProgressReporter : IScanProgressReporter
{
    private static readonly char[] SpinnerFrames = ['|', '/', '-', '\\'];
    private static readonly TimeSpan DefaultSpinnerInterval = TimeSpan.FromMilliseconds(100);

    private readonly object _gate = new();
    private readonly TextWriter _writer;
    private readonly bool _verbose;
    private readonly bool _enableAnimation;
    private readonly TimeSpan _spinnerInterval;
    private readonly Stopwatch _stopwatch = new();

    private CancellationTokenSource? _spinnerCancellation;
    private Task? _spinnerTask;
    private string? _activePhaseName;
    private int _spinnerIndex;
    private int _lastSpinnerLineLength;
    private bool _disposed;

    public ConsoleScanProgressReporter(bool verbose)
        : this(
            verbose,
            Console.Out,
            enableAnimation: ShouldAnimateConsoleOutput(Console.Out),
            spinnerInterval: DefaultSpinnerInterval)
    {
    }

    public ConsoleScanProgressReporter(bool verbose, bool enableAnimation)
        : this(verbose, Console.Out, enableAnimation, DefaultSpinnerInterval)
    {
    }

    public ConsoleScanProgressReporter(
        bool verbose,
        TextWriter writer,
        bool enableAnimation,
        TimeSpan? spinnerInterval = null)
    {
        _verbose = verbose;
        _writer = writer ?? throw new ArgumentNullException(nameof(writer));
        _enableAnimation = enableAnimation;
        _spinnerInterval = spinnerInterval ?? DefaultSpinnerInterval;
    }

    public void ScanStarted(string scanPath, string outputPath)
    {
        ThrowIfDisposed();

        _stopwatch.Restart();

        lock (_gate)
        {
            _writer.WriteLine("LegacyLens.NET");
            _writer.WriteLine();
            _writer.WriteLine($"Scan path: {scanPath}");
            _writer.WriteLine($"Report: {outputPath}");
            _writer.WriteLine();
            _writer.WriteLine("Scanning...");
            _writer.WriteLine();
        }
    }

    public void PhaseStarted(string phaseName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(phaseName);
        ThrowIfDisposed();

        StopActiveSpinner(clearLine: true);

        lock (_gate)
        {
            if (!_enableAnimation)
            {
                _writer.WriteLine($"{NextSpinnerFrame()} {phaseName}...");
                return;
            }

            var spinnerCancellation = new CancellationTokenSource();

            _activePhaseName = phaseName;
            _spinnerCancellation = spinnerCancellation;

            RenderSpinnerFrameLocked();

            _spinnerTask = Task.Run(
                () => RunSpinnerAsync(spinnerCancellation.Token));
        }
    }

    public void PhaseCompleted(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ThrowIfDisposed();

        StopActiveSpinner(clearLine: true);

        lock (_gate)
        {
            _writer.WriteLine($"✓ {message}");
        }
    }

    public void VerboseDetail(string message)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(message);
        ThrowIfDisposed();

        if (!_verbose)
        {
            return;
        }

        var phaseToResume = StopActiveSpinner(clearLine: true);

        lock (_gate)
        {
            _writer.WriteLine($"  {message}");
        }

        if (!string.IsNullOrWhiteSpace(phaseToResume))
        {
            StartActiveSpinner(phaseToResume);
        }
    }

    public void ScanCompleted()
    {
        ThrowIfDisposed();

        StopActiveSpinner(clearLine: true);

        if (!_stopwatch.IsRunning)
        {
            return;
        }

        _stopwatch.Stop();

        lock (_gate)
        {
            _writer.WriteLine();
            _writer.WriteLine($"Completed in {_stopwatch.Elapsed:hh\\:mm\\:ss}");
            _writer.WriteLine();
        }
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        StopActiveSpinner(clearLine: true);
        _stopwatch.Stop();
        _disposed = true;
    }

    private static bool ShouldAnimateConsoleOutput(TextWriter writer)
    {
        return !Console.IsOutputRedirected &&
               writer == Console.Out &&
               writer.GetType() != typeof(StringWriter);
    }

    private async Task RunSpinnerAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                await Task.Delay(_spinnerInterval, cancellationToken).ConfigureAwait(false);

                lock (_gate)
                {
                    if (cancellationToken.IsCancellationRequested || string.IsNullOrWhiteSpace(_activePhaseName))
                    {
                        return;
                    }

                    RenderSpinnerFrameLocked();
                }
            }
        }
        catch (OperationCanceledException)
        {
        }
        catch (ObjectDisposedException)
        {
        }
    }

    private void StartActiveSpinner(string phaseName)
    {
        if (!_enableAnimation)
        {
            return;
        }

        lock (_gate)
        {
            if (_disposed)
            {
                return;
            }

            var spinnerCancellation = new CancellationTokenSource();

            _activePhaseName = phaseName;
            _spinnerCancellation = spinnerCancellation;

            RenderSpinnerFrameLocked();

            _spinnerTask = Task.Run(
                () => RunSpinnerAsync(spinnerCancellation.Token));
        }
    }

    private string? StopActiveSpinner(bool clearLine)
    {
        CancellationTokenSource? spinnerCancellation;
        Task? spinnerTask;
        string? activePhaseName;

        lock (_gate)
        {
            spinnerCancellation = _spinnerCancellation;
            spinnerTask = _spinnerTask;
            activePhaseName = _activePhaseName;

            _spinnerCancellation = null;
            _spinnerTask = null;
            _activePhaseName = null;

            spinnerCancellation?.Cancel();

            if (clearLine && _enableAnimation && _lastSpinnerLineLength > 0)
            {
                ClearActiveLineLocked();
            }
        }

        if (spinnerTask is not null)
        {
            try
            {
                spinnerTask.Wait(TimeSpan.FromSeconds(1));
            }
            catch (AggregateException exception) when (exception.InnerExceptions.All(IsExpectedSpinnerStopException))
            {
            }
            catch (ObjectDisposedException)
            {
            }
        }

        spinnerCancellation?.Dispose();

        return activePhaseName;
    }

    private static bool IsExpectedSpinnerStopException(Exception exception)
    {
        return exception is OperationCanceledException or TaskCanceledException or ObjectDisposedException;
    }

    private void RenderSpinnerFrameLocked()
    {
        if (string.IsNullOrWhiteSpace(_activePhaseName))
        {
            return;
        }

        var line = $"{NextSpinnerFrame()} {_activePhaseName}...";
        _writer.Write('\r');
        _writer.Write(line);

        if (_lastSpinnerLineLength > line.Length)
        {
            _writer.Write(new string(' ', _lastSpinnerLineLength - line.Length));
        }

        _lastSpinnerLineLength = line.Length;
    }

    private void ClearActiveLineLocked()
    {
        _writer.Write('\r');
        _writer.Write(new string(' ', _lastSpinnerLineLength));
        _writer.Write('\r');
        _lastSpinnerLineLength = 0;
    }

    private char NextSpinnerFrame()
    {
        var frame = SpinnerFrames[_spinnerIndex % SpinnerFrames.Length];
        _spinnerIndex++;
        return frame;
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }
}