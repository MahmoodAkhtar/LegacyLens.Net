namespace LegacyLens.Core.Discovery;

public sealed class DiscoveredSolution
{
    public required string Name { get; init; }
    public required string SolutionFilePath { get; init; }
    public List<string> ProjectFilePaths { get; init; } = new();
}