using LegacyLens.Cli.Commands;

namespace LegacyLens.Cli.Progress;

public static class ScanProgressReporterFactory
{
    public static IScanProgressReporter Create(ScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Quiet
            ? NullScanProgressReporter.Instance
            : new ConsoleScanProgressReporter(options.Verbose);
    }
}
