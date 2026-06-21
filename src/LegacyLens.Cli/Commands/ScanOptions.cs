namespace LegacyLens.Cli.Commands;

public sealed class ScanOptions
{
    public const string AllArtifactsSelection = "all";
    public const string UpgradeReadinessArtifact = "upgrade-readiness";
    public const string UpgradeBlockersArtifact = "upgrade-blockers";
    public const string ExternalDependenciesArtifact = "external-dependencies";
    public const string ConfigurationInventoryArtifact = "configuration-inventory";
    public const string DataAccessArtifact = "data-access";
    public const string EdmxAnalysisArtifact = "edmx-analysis";
    public const string ClassDependenciesArtifact = "class-dependencies";
    public const string ClassDependencyScopeArtifact = "class-dependency-scope";
    public const string InterfaceInventoryArtifact = "interface-inventory";
    public const string SolutionTopologyArtifact = "solution-topology";
    public const string CodeComplexityArtifact = "code-complexity";

    public static readonly IReadOnlyList<string> SupportedArtifactNames =
    [
        UpgradeReadinessArtifact,
        UpgradeBlockersArtifact,
        ExternalDependenciesArtifact,
        ConfigurationInventoryArtifact,
        DataAccessArtifact,
        EdmxAnalysisArtifact,
        ClassDependenciesArtifact,
        ClassDependencyScopeArtifact,
        InterfaceInventoryArtifact,
        SolutionTopologyArtifact,
        CodeComplexityArtifact
    ];

    private readonly IReadOnlyList<string> _selectedArtifacts = Array.Empty<string>();

    public required string Path { get; init; }
    public string? Output { get; init; }
    public string? OutputDirectory { get; init; }
    public string Format { get; init; } = "markdown";
    public bool Quiet { get; init; }
    public bool Verbose { get; init; }

    public string? Artifacts { get; init; }
    public string? UpgradeTarget { get; init; }
    public string? ClassDependencyType { get; init; }

    public IReadOnlyList<string> SelectedArtifacts
    {
        get => _selectedArtifacts;
        init => _selectedArtifacts = value
            .Where(artifact => !string.IsNullOrWhiteSpace(artifact))
            .Select(artifact => artifact.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public bool ShouldWriteAllArtifacts { get; init; }

    public bool ShouldWriteUpgradeReadiness => ShouldWriteArtifact(UpgradeReadinessArtifact);

    public bool ShouldWriteUpgradeBlockers => ShouldWriteArtifact(UpgradeBlockersArtifact);

    public bool ShouldWriteExternalDependencies => ShouldWriteArtifact(ExternalDependenciesArtifact);

    public bool ShouldWriteConfigurationInventory => ShouldWriteArtifact(ConfigurationInventoryArtifact);

    public bool ShouldWriteDataAccess => ShouldWriteArtifact(DataAccessArtifact);

    public bool ShouldWriteEdmxAnalysis => ShouldWriteArtifact(EdmxAnalysisArtifact);

    public bool ShouldWriteClassDependencies => ShouldWriteArtifact(ClassDependenciesArtifact);

    public bool ShouldWriteClassDependencyScope => ShouldWriteArtifact(ClassDependencyScopeArtifact);

    public bool ShouldWriteScopedClassDependencyArtifact =>
        !string.IsNullOrWhiteSpace(ClassDependencyType) &&
        (ShouldWriteArtifact(ClassDependencyScopeArtifact) || ShouldWriteAllArtifacts);

    public bool ShouldWriteInterfaceInventory => ShouldWriteArtifact(InterfaceInventoryArtifact);

    public bool ShouldWriteSolutionTopology => ShouldWriteArtifact(SolutionTopologyArtifact);

    public bool ShouldWriteCodeComplexity => ShouldWriteArtifact(CodeComplexityArtifact);

    public bool ShouldWriteArtifact(string artifactName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(artifactName);

        return ShouldWriteAllArtifacts ||
               SelectedArtifacts.Contains(artifactName, StringComparer.OrdinalIgnoreCase);
    }

    public bool HasUpgradeRelatedArtifactSelection =>
        ShouldWriteAllArtifacts ||
        ShouldWriteArtifact(UpgradeReadinessArtifact) ||
        ShouldWriteArtifact(UpgradeBlockersArtifact);

    public bool HasScopedClassDependencyArtifactSelection =>
        ShouldWriteAllArtifacts ||
        ShouldWriteArtifact(ClassDependencyScopeArtifact);
}
