namespace LegacyLens.Core.Analysis;

public sealed class AssemblyUpgradeConsideration
{
    public required string ProjectName { get; init; }
    public required string AssemblyName { get; init; }
    public required string ProjectFilePath { get; init; }
    public required string PossibleConcern { get; init; }
}