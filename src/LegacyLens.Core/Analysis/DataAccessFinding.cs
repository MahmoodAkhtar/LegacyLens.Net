namespace LegacyLens.Core.Analysis;

public sealed record DataAccessFinding(
    DataAccessCategory Category,
    string Name,
    DataAccessSourceType SourceType,
    string SourcePath,
    string? ProjectName,
    string Evidence,
    string? MaskedValue,
    DataAccessConfidence Confidence,
    string MigrationConsideration);

public sealed record DataAccessInventoryReport(
    IReadOnlyList<DataAccessFinding> Findings);