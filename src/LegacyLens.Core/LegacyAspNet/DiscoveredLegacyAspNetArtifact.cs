namespace LegacyLens.Core.LegacyAspNet;

public sealed class DiscoveredLegacyAspNetArtifact
{
    public required LegacyAspNetArtifactKind Kind { get; init; }
    public required string FilePath { get; init; }
    public string? Name { get; init; }
}