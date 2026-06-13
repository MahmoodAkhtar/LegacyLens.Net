namespace LegacyLens.Core.Analysis;

public sealed record ConfigurationInventoryReport(
    IReadOnlyList<ConfigurationInventoryFinding> Findings)
{
    public int FindingCount => Findings.Count;

    public int ConfigurationFileCount => Findings
        .Where(finding => finding.Category == ConfigurationInventoryCategory.ConfigurationFile)
        .Select(finding => finding.SourcePath)
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .Count();

    public int CategoryCount => Findings
        .Select(finding => finding.Category)
        .Distinct()
        .Count();

    public int PotentialMigrationConcernCount => Findings
        .Count(finding => finding.RequiresReview);
}

public sealed record ConfigurationInventoryFinding(
    ConfigurationInventoryCategory Category,
    string Name,
    ConfigurationInventorySourceType SourceType,
    string SourcePath,
    string? ProjectName,
    string Evidence,
    string? MaskedValue,
    ConfigurationInventoryConfidence Confidence,
    bool RequiresReview,
    string MigrationConsideration);

public enum ConfigurationInventoryCategory
{
    ConfigurationFile,
    AppSetting,
    ConnectionString,
    CustomSection,
    EnvironmentTransform,
    WcfConfiguration,
    AspNetIisConfiguration,
    BindingRedirect,
    AuthenticationAuthorization,
    LoggingDiagnostics,
    EntityFrameworkConfiguration,
    SmtpMail,
    ConfigurationApiUsage,
    JsonConfiguration,
    SettingsFile,
    BuildPackageConfiguration,
    UnknownRequiresReview
}

public enum ConfigurationInventorySourceType
{
    Configuration,
    JsonConfiguration,
    SettingsFile,
    Transform,
    NuGetConfig,
    SourceCode,
    ProjectFile,
    Unknown
}

public enum ConfigurationInventoryConfidence
{
    High,
    Medium,
    Low
}