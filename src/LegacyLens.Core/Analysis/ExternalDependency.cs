namespace LegacyLens.Core.Analysis;

public sealed record ExternalDependency(
    ExternalDependencyCategory Category,
    string Name,
    ExternalDependencySourceType SourceType,
    string SourcePath,
    string? ProjectName,
    string Evidence,
    string? MaskedValue,
    ExternalDependencyConfidence Confidence,
    bool RequiresConfirmation,
    string Notes);

public sealed record ExternalDependenciesReport(
    IReadOnlyList<ExternalDependency> Dependencies);