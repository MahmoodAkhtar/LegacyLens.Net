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
    private const string UpgradeReadinessReportFileName = "upgrade-readiness-report.md";
    private const string UpgradeBlockersReportFileName = "upgrade-blockers.md";
    private const string ExternalDependenciesReportFileName = "external-dependencies.md";
    private const string DataAccessInventoryReportFileName = "data-access-inventory.md";
    private const string EdmxAnalysisReportFileName = "edmx-analysis.md";
    private const string ClassDependenciesReportFileName = "class-dependencies.md";

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

        string? upgradeReadinessOutputPath = null;
        UpgradeReadinessReport? upgradeReadinessReport = null;

        if (context.Options.ShouldWriteUpgradeReadiness)
        {
            var upgradeReadinessAnalyzer = new UpgradeReadinessAnalyzer();

            upgradeReadinessReport = upgradeReadinessAnalyzer.Analyze(
                context.Projects,
                context.WcfEndpoints,
                context.WcfServiceContracts,
                context.WcfBehaviours,
                context.LegacyAspNetArtifacts,
                context.ConfigFiles,
                context.ModernisationHints,
                context.Options.UpgradeTarget);

            upgradeReadinessOutputPath = ArtifactOutputPathResolver.Resolve(
                context.ScanPath,
                context.Options,
                UpgradeReadinessReportFileName);

            var upgradeReadinessWriter = new UpgradeReadinessMarkdownReportWriter();
            upgradeReadinessWriter.Write(upgradeReadinessOutputPath, upgradeReadinessReport);
        }

        string? upgradeBlockersOutputPath = null;
        UpgradeBlockersReport? upgradeBlockersReport = null;

        if (context.Options.ShouldWriteUpgradeBlockers)
        {
            var upgradeBlockersAnalyzer = new UpgradeBlockersAnalyzer();

            upgradeBlockersReport = upgradeBlockersAnalyzer.Analyze(
                context.Projects,
                context.WcfEndpoints,
                context.WcfServiceContracts,
                context.WcfBehaviours,
                context.LegacyAspNetArtifacts,
                context.ConfigFiles,
                context.ModernisationHints,
                context.Options.UpgradeTarget);

            upgradeBlockersOutputPath = ArtifactOutputPathResolver.Resolve(
                context.ScanPath,
                context.Options,
                UpgradeBlockersReportFileName);

            var upgradeBlockersWriter = new UpgradeBlockersMarkdownReportWriter();
            upgradeBlockersWriter.Write(upgradeBlockersOutputPath, upgradeBlockersReport);
        }

        string? externalDependenciesOutputPath = null;
        ExternalDependenciesReport? externalDependenciesReport = null;

        if (context.Options.ShouldWriteExternalDependencies)
        {
            var externalDependenciesAnalyzer = new ExternalDependenciesAnalyzer();

            externalDependenciesReport = externalDependenciesAnalyzer.Analyze(
                context.Projects,
                context.WcfEndpoints,
                context.ConfigFiles);

            externalDependenciesOutputPath = ArtifactOutputPathResolver.Resolve(
                context.ScanPath,
                context.Options,
                ExternalDependenciesReportFileName);

            var externalDependenciesWriter = new ExternalDependenciesMarkdownReportWriter();
            externalDependenciesWriter.Write(externalDependenciesOutputPath, externalDependenciesReport);
        }

        string? dataAccessOutputPath = null;
        DataAccessInventoryReport? dataAccessReport = null;

        if (context.Options.ShouldWriteDataAccess)
        {
            var dataAccessAnalyzer = new DataAccessAnalyzer();

            dataAccessReport = dataAccessAnalyzer.Analyze(
                context.Projects,
                context.ConfigFiles);

            dataAccessOutputPath = ArtifactOutputPathResolver.Resolve(
                context.ScanPath,
                context.Options,
                DataAccessInventoryReportFileName);

            var dataAccessWriter = new DataAccessInventoryMarkdownReportWriter();
            dataAccessWriter.Write(dataAccessOutputPath, dataAccessReport);
        }

        string? edmxAnalysisOutputPath = null;
        EdmxAnalysisReport? edmxAnalysisReport = null;

        if (context.Options.ShouldWriteEdmxAnalysis)
        {
            var edmxAnalyzer = new EdmxAnalyzer();

            edmxAnalysisReport = edmxAnalyzer.Analyze(
                context.ScanPath,
                context.Projects);

            edmxAnalysisOutputPath = ArtifactOutputPathResolver.Resolve(
                context.ScanPath,
                context.Options,
                EdmxAnalysisReportFileName);

            var edmxAnalysisWriter = new EdmxAnalysisMarkdownReportWriter();
            edmxAnalysisWriter.Write(edmxAnalysisOutputPath, edmxAnalysisReport);
        }

        string? classDependenciesOutputPath = null;
        ClassDependencyReport? classDependenciesReport = null;

        if (context.Options.ShouldWriteClassDependencies)
        {
            var classDependencyAnalyzer = new ClassDependencyAnalyzer();

            classDependenciesReport = classDependencyAnalyzer.Analyze(context.Projects);

            classDependenciesOutputPath = ArtifactOutputPathResolver.Resolve(
                context.ScanPath,
                context.Options,
                ClassDependenciesReportFileName);

            var classDependenciesWriter = new ClassDependenciesMarkdownReportWriter();
            classDependenciesWriter.Write(classDependenciesOutputPath, classDependenciesReport);
        }

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
            UpgradeReadinessOutputPath = upgradeReadinessOutputPath,
            UpgradeReadinessReport = upgradeReadinessReport,
            UpgradeBlockersOutputPath = upgradeBlockersOutputPath,
            UpgradeBlockersReport = upgradeBlockersReport,
            ExternalDependenciesOutputPath = externalDependenciesOutputPath,
            ExternalDependenciesReport = externalDependenciesReport,
            DataAccessOutputPath = dataAccessOutputPath,
            DataAccessReport = dataAccessReport,
            EdmxAnalysisOutputPath = edmxAnalysisOutputPath,
            EdmxAnalysisReport = edmxAnalysisReport,
            ClassDependenciesOutputPath = classDependenciesOutputPath,
            ClassDependenciesReport = classDependenciesReport
        };
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