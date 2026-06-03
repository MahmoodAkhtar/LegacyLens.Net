namespace LegacyLens.Core.Analysis;

public sealed class ModernisationReviewArea
{
    public required string Area { get; init; }
    public required ModernisationHintSeverity HighestSeverity { get; init; }
    public int RiskCount { get; init; }
    public int WarningCount { get; init; }
    public int InfoCount { get; init; }
    public int TotalCount => RiskCount + WarningCount + InfoCount;
    public required string Summary { get; init; }
}