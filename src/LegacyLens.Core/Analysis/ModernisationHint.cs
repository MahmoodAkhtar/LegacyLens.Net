namespace LegacyLens.Core.Analysis;

public sealed class ModernisationHint
{
    public required ModernisationHintSeverity Severity { get; init; }
    public required string Area { get; init; }
    public required string Finding { get; init; }
    public required string Reason { get; init; }

    public string? EvidenceKind { get; init; }
    public string? EvidenceName { get; init; }
    public string? EvidencePath { get; init; }
    public ModernisationHintConfidence Confidence { get; init; } = ModernisationHintConfidence.High;
}