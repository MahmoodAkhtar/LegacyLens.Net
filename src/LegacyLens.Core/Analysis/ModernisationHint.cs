namespace LegacyLens.Core.Analysis;

public sealed class ModernisationHint
{
    public required ModernisationHintSeverity Severity { get; init; }
    public required string Area { get; init; }
    public required string Finding { get; init; }
    public required string Reason { get; init; }
}