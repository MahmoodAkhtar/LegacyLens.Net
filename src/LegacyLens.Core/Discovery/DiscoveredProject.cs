namespace LegacyLens.Core.Discovery;

public sealed class DiscoveredProject
{
    public required string Name { get; init; }
    public required string ProjectFilePath { get; init; }
    public string? TargetFramework { get; init; }
    public List<string> ProjectReferences { get; init; } = new();
    public List<string> PackageReferences { get; init; } = new();
    public List<DiscoveredPackageReference> PackageReferenceDetails { get; init; } = new();
    public List<string> AssemblyReferences { get; init; } = new();
}
