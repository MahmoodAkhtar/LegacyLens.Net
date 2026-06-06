namespace LegacyLens.Core.Discovery;

public sealed class DiscoveredPackageReference
{
    public required string Name { get; init; }
    public string? Version { get; init; }
    public required string SourceFormat { get; init; }
    public required string SourcePath { get; init; }
    public string? PackageTargetFramework { get; init; }
}
