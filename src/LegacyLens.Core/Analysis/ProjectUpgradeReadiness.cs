namespace LegacyLens.Core.Analysis;

public sealed class ProjectUpgradeReadiness
{
    public required string ProjectName { get; init; }
    public string? CurrentTargetFramework { get; init; }
    public required string ProjectFilePath { get; init; }
    public required UpgradeReadinessLevel Readiness { get; init; }
    public required string Reason { get; init; }
}