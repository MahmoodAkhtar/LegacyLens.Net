namespace LegacyLens.Core.Analysis;

public sealed record ConfigurationInventoryReport(
    IReadOnlyList<ConfigurationInventoryFinding> Findings,
    IReadOnlyList<ConfigurationUsageFinding> SourceUsages,
    IReadOnlyList<ConfigurationKeyReconciliation> KeyReconciliations)
{
    public ConfigurationInventoryReport(IReadOnlyList<ConfigurationInventoryFinding> findings)
        : this(
            findings,
            Array.Empty<ConfigurationUsageFinding>(),
            Array.Empty<ConfigurationKeyReconciliation>())
    {
    }

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

    public int SourceUsageCount => SourceUsages.Count;

    public int DynamicSourceUsageCount => SourceUsages.Count(usage => usage.RequiresReview);

    public int MatchedSourceUsageCount => SourceUsages.Count(usage =>
        usage.Resolution == ConfigurationUsageKeyResolution.MatchedVisibleConfigurationEntry);

    public int SourceUsageWithoutVisibleConfigurationCount => SourceUsages.Count(usage =>
        usage.Resolution == ConfigurationUsageKeyResolution.NoVisibleConfigurationEntryFound);

    public int ConfiguredKeyWithoutStaticSourceUsageCount => KeyReconciliations.Count(reconciliation =>
        reconciliation.StaticSourceUsage == ConfigurationStaticSourceUsage.NoStaticSourceUsageDetected);
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

public sealed record ConfigurationUsageFinding(
    ConfigurationUsageKind Kind,
    string? Key,
    ConfigurationUsageKeyResolution Resolution,
    string ProjectName,
    string SourcePath,
    int LineNumber,
    string Evidence,
    bool RequiresReview);

public sealed record ConfigurationKeyReconciliation(
    ConfigurationUsageKind Kind,
    string Key,
    string ConfigSourcePath,
    ConfigurationStaticSourceUsage StaticSourceUsage,
    string Notes);

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

public enum ConfigurationUsageKind
{
    AppSetting,
    ConnectionString
}

public enum ConfigurationUsageKeyResolution
{
    MatchedVisibleConfigurationEntry,
    NoVisibleConfigurationEntryFound,
    DynamicKeyRequiresReview
}

public enum ConfigurationStaticSourceUsage
{
    Found,
    NoStaticSourceUsageDetected
}
