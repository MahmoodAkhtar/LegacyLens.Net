namespace LegacyLens.Core.Analysis;

public sealed class UpgradeConcern
{
    public required string Concern { get; init; }
    public required string Evidence { get; init; }
    public required string WhyItMatters { get; init; }
}