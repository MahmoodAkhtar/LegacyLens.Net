namespace LegacyLens.Core.Analysis;

public sealed class UpgradeBlocker
{
    public required int Priority { get; init; }
    public required UpgradeBlockerCategory Category { get; init; }
    public required UpgradeBlockerImpact Impact { get; init; }
    public required string Title { get; init; }
    public required string WhyItMatters { get; init; }
    public IReadOnlyList<string> DecisionsRequired { get; init; } = Array.Empty<string>();
    public IReadOnlyList<UpgradeBlockerEvidence> Evidence { get; init; } = Array.Empty<UpgradeBlockerEvidence>();
}