namespace LegacyLens.Cli.Progress;

public interface IScanProgressReporter
{
    void ScanStarted(string scanPath, string outputPath);

    void PhaseStarted(string phaseName);

    void PhaseCompleted(string message);

    void VerboseDetail(string message);

    void ScanCompleted();
}
