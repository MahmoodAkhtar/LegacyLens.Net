namespace LegacyLens.Cli.Commands.Runners;

public sealed record ScanArtifactResult(
    string ArtifactName,
    string OutputPath,
    object Report);