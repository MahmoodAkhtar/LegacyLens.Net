using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Cli.Commands;

public sealed class ScanResult
{
    public required string ScanPath { get; init; }
    public required string OutputPath { get; init; }

    public required IReadOnlyList<DiscoveredSolution> Solutions { get; init; }
    public required IReadOnlyList<DiscoveredProject> Projects { get; init; }
    public required IReadOnlyList<WcfEndpoint> WcfEndpoints { get; init; }
    public required IReadOnlyList<WcfServiceContract> WcfServiceContracts { get; init; }
    public required IReadOnlyList<WcfBehaviour> WcfBehaviours { get; init; }
    public required IReadOnlyList<DiscoveredLegacyAspNetArtifact> LegacyAspNetArtifacts { get; init; }
    public required IReadOnlyList<DiscoveredConfigFile> ConfigFiles { get; init; }
    public required IReadOnlyList<ModernisationHint> ModernisationHints { get; init; }
    public required IReadOnlyList<ModernisationReviewArea> ModernisationReviewAreas { get; init; }

    public string? UpgradeReadinessOutputPath { get; init; }
    public UpgradeReadinessReport? UpgradeReadinessReport { get; init; }

    public string? UpgradeBlockersOutputPath { get; init; }
    public UpgradeBlockersReport? UpgradeBlockersReport { get; init; }

    public string? ExternalDependenciesOutputPath { get; init; }
    public ExternalDependenciesReport? ExternalDependenciesReport { get; init; }

    public string? ConfigurationInventoryOutputPath { get; init; }
    public ConfigurationInventoryReport? ConfigurationInventoryReport { get; init; }
    
    public string? DataAccessOutputPath { get; init; }
    public DataAccessInventoryReport? DataAccessReport { get; init; }
    
    public string? EdmxAnalysisOutputPath { get; init; }
    public EdmxAnalysisReport? EdmxAnalysisReport { get; init; }
    
    public string? ClassDependenciesOutputPath { get; init; }
    public ClassDependencyReport? ClassDependenciesReport { get; init; }

    public string? ScopedClassDependencyOutputPath { get; init; }
    public ScopedClassDependencyReport? ScopedClassDependencyReport { get; init; }

    public string? ClassRefactoringOpportunitiesOutputPath { get; init; }
    public ClassRefactoringOpportunitiesReport? ClassRefactoringOpportunitiesReport { get; init; }

    public string? InterfaceInventoryOutputPath { get; init; }
    public InterfaceInventoryReport? InterfaceInventoryReport { get; init; }

    public string? SolutionTopologyOutputPath { get; init; }
    public SolutionTopologyReport? SolutionTopologyReport { get; init; }

    public string? CodeComplexityOutputPath { get; init; }
    public CodeComplexityReport? CodeComplexityReport { get; init; }

    public int ProjectReferenceCount => Projects.Sum(x => x.ProjectReferences.Count);
    public int PackageReferenceCount => Projects.Sum(x => x.PackageReferences.Count);
    public int AssemblyReferenceCount => Projects.Sum(x => x.AssemblyReferences.Count);
}
