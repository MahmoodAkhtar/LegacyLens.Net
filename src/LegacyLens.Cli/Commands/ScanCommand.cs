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

        var discoveryReportWriter = new DiscoveryMarkdownReportWriter();
        discoveryReportWriter.Write(
            outputPath,
            solutions,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            wcfBehaviours,
            legacyAspNetArtifacts,
            modernisationHints,
            configFiles);

        string? upgradeReadinessOutputPath = null;
        UpgradeReadinessReport? upgradeReadinessReport = null;

        if (options.ShouldWriteUpgradeReadiness)
        {
            var upgradeReadinessAnalyzer = new UpgradeReadinessAnalyzer();

            upgradeReadinessReport = upgradeReadinessAnalyzer.Analyze(
                projects,
                wcfEndpoints,
                wcfServiceContracts,
                wcfBehaviours,
                legacyAspNetArtifacts,
                configFiles,
                modernisationHints,
                options.UpgradeTarget);

            upgradeReadinessOutputPath = ArtifactOutputPathResolver.Resolve(
                scanPath,
                options,
                UpgradeReadinessReportFileName);

            var upgradeReadinessWriter = new UpgradeReadinessMarkdownReportWriter();
            upgradeReadinessWriter.Write(upgradeReadinessOutputPath, upgradeReadinessReport);
        }

        string? upgradeBlockersOutputPath = null;
        UpgradeBlockersReport? upgradeBlockersReport = null;

        if (options.ShouldWriteUpgradeBlockers)
        {
            var upgradeBlockersAnalyzer = new UpgradeBlockersAnalyzer();

            upgradeBlockersReport = upgradeBlockersAnalyzer.Analyze(
                projects,
                wcfEndpoints,
                wcfServiceContracts,
                wcfBehaviours,
                legacyAspNetArtifacts,
                configFiles,
                modernisationHints,
                options.UpgradeTarget);

            upgradeBlockersOutputPath = ArtifactOutputPathResolver.Resolve(
                scanPath,
                options,
                UpgradeBlockersReportFileName);

            var upgradeBlockersWriter = new UpgradeBlockersMarkdownReportWriter();
            upgradeBlockersWriter.Write(upgradeBlockersOutputPath, upgradeBlockersReport);
        }

        string? externalDependenciesOutputPath = null;
        ExternalDependenciesReport? externalDependenciesReport = null;

        if (options.ShouldWriteExternalDependencies)
        {
            var externalDependenciesAnalyzer = new ExternalDependenciesAnalyzer();

            externalDependenciesReport = externalDependenciesAnalyzer.Analyze(
                projects,
                wcfEndpoints,
                configFiles);

            externalDependenciesOutputPath = ArtifactOutputPathResolver.Resolve(
                scanPath,
                options,
                ExternalDependenciesReportFileName);

            var externalDependenciesWriter = new ExternalDependenciesMarkdownReportWriter();
            externalDependenciesWriter.Write(externalDependenciesOutputPath, externalDependenciesReport);
        }
        
        string? dataAccessOutputPath = null;
        DataAccessInventoryReport? dataAccessReport = null;

        if (options.ShouldWriteDataAccess)
        {
            var dataAccessAnalyzer = new DataAccessAnalyzer();

            dataAccessReport = dataAccessAnalyzer.Analyze(projects, configFiles);

            dataAccessOutputPath = ArtifactOutputPathResolver.Resolve(
                scanPath,
                options,
                DataAccessInventoryReportFileName);

            var dataAccessWriter = new DataAccessInventoryMarkdownReportWriter();
            dataAccessWriter.Write(dataAccessOutputPath, dataAccessReport);
        }
        
        string? edmxAnalysisOutputPath = null;
        EdmxAnalysisReport? edmxAnalysisReport = null;

        if (options.ShouldWriteEdmxAnalysis)
        {
            var edmxAnalyzer = new EdmxAnalyzer();

            edmxAnalysisReport = edmxAnalyzer.Analyze(scanPath, projects);
            
            edmxAnalysisOutputPath = ArtifactOutputPathResolver.Resolve(
                scanPath,
                options,
                EdmxAnalysisReportFileName);

            var edmxAnalysisWriter = new EdmxAnalysisMarkdownReportWriter();
            edmxAnalysisWriter.Write(edmxAnalysisOutputPath, edmxAnalysisReport);
        }
        
        string? classDependenciesOutputPath = null;
        ClassDependencyReport? classDependenciesReport = null;

        if (options.ShouldWriteClassDependencies)
        {
            var classDependencyAnalyzer = new ClassDependencyAnalyzer();

            classDependenciesReport = classDependencyAnalyzer.Analyze(projects);
            
            classDependenciesOutputPath = ArtifactOutputPathResolver.Resolve(
                scanPath,
                options,
                ClassDependenciesReportFileName);

            var classDependenciesWriter = new ClassDependenciesMarkdownReportWriter();
            classDependenciesWriter.Write(classDependenciesOutputPath, classDependenciesReport);
        }

        return new ScanResult
        {
            ScanPath = scanPath,
            OutputPath = outputPath,
            Solutions = solutions,
            Projects = projects,
            WcfEndpoints = wcfEndpoints,
            WcfServiceContracts = wcfServiceContracts,
            WcfBehaviours = wcfBehaviours,
            LegacyAspNetArtifacts = legacyAspNetArtifacts,
            ConfigFiles = configFiles,
            ModernisationHints = modernisationHints,
            ModernisationReviewAreas = modernisationReviewAreas,
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