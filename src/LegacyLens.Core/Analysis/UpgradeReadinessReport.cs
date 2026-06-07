namespace LegacyLens.Core.Analysis;

public sealed class UpgradeReadinessReport
{
    public string? RequestedUpgradeTarget { get; init; }

    public IReadOnlyList<UpgradeReadinessOverviewItem> Overview { get; init; } =
        Array.Empty<UpgradeReadinessOverviewItem>();

    public IReadOnlyList<ProjectUpgradeReadiness> ProjectReadiness { get; init; } =
        Array.Empty<ProjectUpgradeReadiness>();

    public IReadOnlyList<UpgradeConcern> Concerns { get; init; } =
        Array.Empty<UpgradeConcern>();

    public IReadOnlyList<PackageUpgradeConsideration> PackageConsiderations { get; init; } =
        Array.Empty<PackageUpgradeConsideration>();

    public IReadOnlyList<AssemblyUpgradeConsideration> AssemblyConsiderations { get; init; } =
        Array.Empty<AssemblyUpgradeConsideration>();

    public IReadOnlyList<ConfigurationRuntimeConsideration> ConfigurationRuntimeConsiderations { get; init; } =
        Array.Empty<ConfigurationRuntimeConsideration>();
}