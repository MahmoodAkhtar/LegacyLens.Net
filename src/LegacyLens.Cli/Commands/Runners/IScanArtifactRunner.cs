namespace LegacyLens.Cli.Commands.Runners;

public interface IScanArtifactRunner
{
    string ArtifactName { get; }

    bool ShouldRun(ScanContext context);

    ScanArtifactResult Run(ScanContext context);
}