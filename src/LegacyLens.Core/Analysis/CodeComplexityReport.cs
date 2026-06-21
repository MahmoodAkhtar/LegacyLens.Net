namespace LegacyLens.Core.Analysis;

public sealed record CodeComplexityReport(
    CodeComplexityScanSummary Summary,
    IReadOnlyList<CodeComplexityMember> Members,
    IReadOnlyList<CodeComplexityTypeSummary> TypeSummaries,
    IReadOnlyList<CodeComplexityNamespaceSummary> NamespaceSummaries,
    IReadOnlyList<CodeComplexityProjectSummary> ProjectSummaries);

public sealed record CodeComplexityScanSummary(
    int SourceFileCount,
    int MemberCount,
    int GeneratedMemberCount,
    int TotalComplexity,
    double AverageComplexity,
    int HighComplexityMemberCount,
    int VeryHighComplexityMemberCount,
    int HighComplexityTypeCount,
    int VeryHighComplexityTypeCount);

public sealed record CodeComplexityMember(
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string NamespaceName,
    string TypeName,
    string MemberName,
    string MemberKind,
    int Complexity,
    CodeComplexitySeverity Severity,
    bool IsLikelyGeneratedCode,
    string Evidence);

public sealed record CodeComplexityTypeSummary(
    string ProjectName,
    string SourcePath,
    string NamespaceName,
    string TypeName,
    int MemberCount,
    int TotalComplexity,
    double AverageComplexity,
    int MaximumMemberComplexity,
    CodeComplexitySeverity HighestSeverity,
    bool ContainsLikelyGeneratedCode);

public sealed record CodeComplexityNamespaceSummary(
    string ProjectName,
    string NamespaceName,
    int TypeCount,
    int MemberCount,
    int TotalComplexity,
    double AverageComplexity,
    int MaximumMemberComplexity,
    CodeComplexitySeverity HighestSeverity);

public sealed record CodeComplexityProjectSummary(
    string ProjectName,
    int NamespaceCount,
    int TypeCount,
    int MemberCount,
    int TotalComplexity,
    double AverageComplexity,
    int MaximumMemberComplexity,
    CodeComplexitySeverity HighestSeverity,
    int GeneratedMemberCount);

public enum CodeComplexitySeverity
{
    Low = 0,
    Moderate = 1,
    High = 2,
    VeryHigh = 3
}
