using LegacyLens.Cli.Commands.Runners;
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands;

public sealed class ScanCommand
{
    private const string DiscoveryReportFileName = "discovery-report.md";

    public ScanResult Execute(ScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var scanPath = Path.GetFullPath(options.Path);

        if (!Directory.Exists(scanPath))
        {
            throw new DirectoryNotFoundException($"Scan path does not exist: {scanPath}");
        }

        var outputPath = ResolveOutputPath(scanPath, options);

        var projectDiscoveryService = new ProjectDiscoveryService();
        var projects = projectDiscoveryService.DiscoverProjects(scanPath);

        var solutionDiscoveryService = new SolutionDiscoveryService();
        var solutions = solutionDiscoveryService.DiscoverSolutions(scanPath);

        var wcfConfigScanner = new WcfConfigScanner();
        var wcfEndpoints = wcfConfigScanner.Scan(scanPath);
        var wcfBehaviours = wcfConfigScanner.ScanBehaviours(scanPath);

        var wcfServiceContractScanner = new WcfServiceContractScanner();
        var wcfServiceContracts = wcfServiceContractScanner.Scan(scanPath);

        var configFileScanner = new ConfigFileScanner();
        var configFiles = configFileScanner.Scan(scanPath);

        var legacyAspNetArtifactScanner = new LegacyAspNetArtifactScanner();
        var legacyAspNetArtifacts = legacyAspNetArtifactScanner.Scan(scanPath);

        var modernisationHintAnalyzer = new ModernisationHintAnalyzer();
        var modernisationHints = modernisationHintAnalyzer.Analyze(
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            wcfBehaviours,
            legacyAspNetArtifacts,
            configFiles);

        var modernisationReviewPrioritiser = new ModernisationReviewPrioritiser();
        var modernisationReviewAreas = modernisationReviewPrioritiser.Prioritise(modernisationHints);

        var context = new ScanContext(
            scanPath,
            outputPath,
            options,
            solutions,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            wcfBehaviours,
            legacyAspNetArtifacts,
            configFiles,
            modernisationHints,
            modernisationReviewAreas);

        var discoveryReportWriter = new DiscoveryMarkdownReportWriter();
        discoveryReportWriter.Write(
            context.OutputPath,
            context.Solutions,
            context.Projects,
            context.WcfEndpoints,
            context.WcfServiceContracts,
            context.WcfBehaviours,
            context.LegacyAspNetArtifacts,
            context.ModernisationHints,
            context.ConfigFiles);

        var artifactResults = CreateArtifactRunners()
            .Where(runner => runner.ShouldRun(context))
            .Select(runner => runner.Run(context))
            .ToArray();

        var upgradeReadinessResult = FindArtifactResult(artifactResults, "upgrade-readiness");
        var upgradeBlockersResult = FindArtifactResult(artifactResults, "upgrade-blockers");
        var externalDependenciesResult = FindArtifactResult(artifactResults, "external-dependencies");
        var dataAccessResult = FindArtifactResult(artifactResults, "data-access");
        var edmxAnalysisResult = FindArtifactResult(artifactResults, "edmx-analysis");
        var classDependenciesResult = FindArtifactResult(artifactResults, "class-dependencies");

        return new ScanResult
        {
            ScanPath = context.ScanPath,
            OutputPath = context.OutputPath,
            Solutions = context.Solutions,
            Projects = context.Projects,
            WcfEndpoints = context.WcfEndpoints,
            WcfServiceContracts = context.WcfServiceContracts,
            WcfBehaviours = context.WcfBehaviours,
            LegacyAspNetArtifacts = context.LegacyAspNetArtifacts,
            ConfigFiles = context.ConfigFiles,
            ModernisationHints = context.ModernisationHints,
            ModernisationReviewAreas = context.ModernisationReviewAreas,

            UpgradeReadinessOutputPath = upgradeReadinessResult?.OutputPath,
            UpgradeReadinessReport = upgradeReadinessResult?.Report as UpgradeReadinessReport,

            UpgradeBlockersOutputPath = upgradeBlockersResult?.OutputPath,
            UpgradeBlockersReport = upgradeBlockersResult?.Report as UpgradeBlockersReport,

            ExternalDependenciesOutputPath = externalDependenciesResult?.OutputPath,
            ExternalDependenciesReport = externalDependenciesResult?.Report as ExternalDependenciesReport,

            DataAccessOutputPath = dataAccessResult?.OutputPath,
            DataAccessReport = dataAccessResult?.Report as DataAccessInventoryReport,

            EdmxAnalysisOutputPath = edmxAnalysisResult?.OutputPath,
            EdmxAnalysisReport = edmxAnalysisResult?.Report as EdmxAnalysisReport,

            ClassDependenciesOutputPath = classDependenciesResult?.OutputPath,
            ClassDependenciesReport = classDependenciesResult?.Report as ClassDependencyReport
        };
    }

    private static IReadOnlyList<IScanArtifactRunner> CreateArtifactRunners() =>
    [
        new UpgradeReadinessArtifactRunner(),
        new UpgradeBlockersArtifactRunner(),
        new ExternalDependenciesArtifactRunner(),
        new DataAccessArtifactRunner(),
        new EdmxAnalysisArtifactRunner(),
        new ClassDependenciesArtifactRunner()
    ];

    private static ScanArtifactResult? FindArtifactResult(
        IEnumerable<ScanArtifactResult> artifactResults,
        string artifactName)
    {
        return artifactResults.SingleOrDefault(
            result => result.ArtifactName.Equals(
                artifactName,
                StringComparison.OrdinalIgnoreCase));
    }

    private static string ResolveOutputPath(string scanPath, ScanOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            return Path.GetFullPath(options.Output);
        }

        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return Path.Combine(
                Path.GetFullPath(options.OutputDirectory),
                DiscoveryReportFileName);
        }

        return Path.Combine(scanPath, "output", DiscoveryReportFileName);
    }
}