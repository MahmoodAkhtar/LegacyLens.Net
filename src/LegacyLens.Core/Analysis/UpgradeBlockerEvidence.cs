namespace LegacyLens.Core.Analysis;

public sealed class UpgradeBlockerEvidence
{
    public string? ProjectName { get; init; }
    public required string Source { get; init; }
    public required string Finding { get; init; }
}