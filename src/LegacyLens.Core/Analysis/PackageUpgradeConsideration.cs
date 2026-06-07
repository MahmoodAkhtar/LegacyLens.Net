namespace LegacyLens.Core.Analysis;

public sealed class PackageUpgradeConsideration
{
    public required string ProjectName { get; init; }
    public required string PackageName { get; init; }
    public string? Version { get; init; }
    public string? ProjectTargetFramework { get; init; }
    public string? PackageTargetFramework { get; init; }
    public required string SourceFormat { get; init; }
    public required string SourcePath { get; init; }
    public required string PossibleConcern { get; init; }
}