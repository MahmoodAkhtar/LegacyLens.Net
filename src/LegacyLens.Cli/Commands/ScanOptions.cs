namespace LegacyLens.Cli.Commands;

public sealed class ScanOptions
{
    public required string Path { get; init; }
    public string? Output { get; init; }
    public string? OutputDirectory { get; init; }
    public string Format { get; init; } = "markdown";
    public bool Quiet { get; init; }
    public bool Verbose { get; init; }

    public string? Artifacts { get; init; }
    public string? UpgradeTarget { get; init; }

    public bool ShouldWriteUpgradeReadiness =>
        string.Equals(Artifacts, "upgrade-readiness", StringComparison.OrdinalIgnoreCase);

    public bool ShouldWriteUpgradeBlockers =>
        string.Equals(Artifacts, "upgrade-blockers", StringComparison.OrdinalIgnoreCase);

    public bool ShouldWriteExternalDependencies =>
        string.Equals(Artifacts, "external-dependencies", StringComparison.OrdinalIgnoreCase);

    public bool ShouldWriteConfigurationInventory =>
        string.Equals(Artifacts, "configuration-inventory", StringComparison.OrdinalIgnoreCase);
    
    public bool ShouldWriteDataAccess =>
        string.Equals(Artifacts, "data-access", StringComparison.OrdinalIgnoreCase);
    
    public bool ShouldWriteEdmxAnalysis =>
        string.Equals(Artifacts, "edmx-analysis", StringComparison.OrdinalIgnoreCase);
    
    public bool ShouldWriteClassDependencies =>
        string.Equals(Artifacts, "class-dependencies", StringComparison.OrdinalIgnoreCase);

    public bool ShouldWriteSolutionTopology =>
        string.Equals(Artifacts, "solution-topology", StringComparison.OrdinalIgnoreCase);
}