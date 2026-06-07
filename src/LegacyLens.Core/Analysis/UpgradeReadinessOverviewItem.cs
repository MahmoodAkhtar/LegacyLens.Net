namespace LegacyLens.Core.Analysis;

public sealed class UpgradeReadinessOverviewItem
{
    public required string Area { get; init; }
    public required string Status { get; init; }
    public required string Evidence { get; init; }
}