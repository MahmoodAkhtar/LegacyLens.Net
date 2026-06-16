namespace LegacyLens.Cli.Progress;

public sealed class NullScanProgressReporter : IScanProgressReporter
{
    public static NullScanProgressReporter Instance { get; } = new();

    private NullScanProgressReporter()
    {
    }

    public void ScanStarted(string scanPath, string outputPath)
    {
    }

    public void PhaseStarted(string phaseName)
    {
    }

    public void PhaseCompleted(string message)
    {
    }

    public void VerboseDetail(string message)
    {
    }

    public void ScanCompleted()
    {
    }
}
