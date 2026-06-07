namespace LegacyLens.Core.Analysis;

public sealed class ConfigurationRuntimeConsideration
{
    public required string Source { get; init; }
    public required string Finding { get; init; }
    public required string PossibleConcern { get; init; }
}