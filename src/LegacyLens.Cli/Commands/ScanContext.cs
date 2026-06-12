using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Cli.Commands;

public sealed record ScanContext(
    string ScanPath,
    string OutputPath,
    ScanOptions Options,
    IReadOnlyList<DiscoveredSolution> Solutions,
    IReadOnlyList<DiscoveredProject> Projects,
    IReadOnlyList<WcfEndpoint> WcfEndpoints,
    IReadOnlyList<WcfServiceContract> WcfServiceContracts,
    IReadOnlyList<WcfBehaviour> WcfBehaviours,
    IReadOnlyList<DiscoveredLegacyAspNetArtifact> LegacyAspNetArtifacts,
    IReadOnlyList<DiscoveredConfigFile> ConfigFiles,
    IReadOnlyList<ModernisationHint> ModernisationHints,
    IReadOnlyList<ModernisationReviewArea> ModernisationReviewAreas,
    ScanFileInventory FileInventory);