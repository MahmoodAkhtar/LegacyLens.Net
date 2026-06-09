using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands;

public sealed class ScanCommand
{
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

        var reportWriter = new MarkdownReportWriter();
        reportWriter.Write(
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

            upgradeReadinessOutputPath = ResolveUpgradeReadinessOutputPath(scanPath, options);

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

            upgradeBlockersOutputPath = ResolveUpgradeBlockersOutputPath(scanPath, options);

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

            externalDependenciesOutputPath = ResolveExternalDependenciesOutputPath(scanPath, options);

            var externalDependenciesWriter = new ExternalDependenciesMarkdownReportWriter();
            externalDependenciesWriter.Write(externalDependenciesOutputPath, externalDependenciesReport);
        }
        
        string? dataAccessOutputPath = null;
        DataAccessInventoryReport? dataAccessReport = null;

        if (options.ShouldWriteDataAccess)
        {
            var dataAccessAnalyzer = new DataAccessAnalyzer();

            dataAccessReport = dataAccessAnalyzer.Analyze(
                projects,
                configFiles);

            dataAccessOutputPath = ResolveDataAccessOutputPath(scanPath, options);

            var dataAccessWriter = new DataAccessInventoryMarkdownReportWriter();
            dataAccessWriter.Write(dataAccessOutputPath, dataAccessReport);
        }
        
        string? edmxAnalysisOutputPath = null;
        EdmxAnalysisReport? edmxAnalysisReport = null;

        if (options.ShouldWriteEdmxAnalysis)
        {
            var edmxAnalyzer = new EdmxAnalyzer();

            edmxAnalysisReport = edmxAnalyzer.Analyze(scanPath, projects);
            edmxAnalysisOutputPath = ResolveEdmxAnalysisOutputPath(scanPath, options);

            var edmxAnalysisWriter = new EdmxAnalysisMarkdownReportWriter();
            edmxAnalysisWriter.Write(edmxAnalysisOutputPath, edmxAnalysisReport);
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
            EdmxAnalysisReport = edmxAnalysisReport
        };
    }
    
    private static string ResolveEdmxAnalysisOutputPath(string scanPath, ScanOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return Path.Combine(
                Path.GetFullPath(options.OutputDirectory),
                "edmx-analysis.md");
        }

        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.Output));

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                return Path.Combine(outputDirectory, "edmx-analysis.md");
            }
        }

        return Path.Combine(scanPath, "output", "edmx-analysis.md");
    }

    private static string ResolveDataAccessOutputPath(string scanPath, ScanOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return Path.Combine(
                Path.GetFullPath(options.OutputDirectory),
                "data-access-inventory.md");
        }

        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.Output));

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                return Path.Combine(outputDirectory, "data-access-inventory.md");
            }
        }

        return Path.Combine(scanPath, "output", "data-access-inventory.md");
    }
    
    private static string ResolveExternalDependenciesOutputPath(string scanPath, ScanOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return Path.Combine(
                Path.GetFullPath(options.OutputDirectory),
                "external-dependencies.md");
        }

        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.Output));

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                return Path.Combine(outputDirectory, "external-dependencies.md");
            }
        }

        return Path.Combine(scanPath, "output", "external-dependencies.md");
    }

    private static string ResolveUpgradeBlockersOutputPath(string scanPath, ScanOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return Path.Combine(
                Path.GetFullPath(options.OutputDirectory),
                "upgrade-blockers.md");
        }

        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.Output));

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                return Path.Combine(outputDirectory, "upgrade-blockers.md");
            }
        }

        return Path.Combine(scanPath, "output", "upgrade-blockers.md");
    }

    private static string ResolveUpgradeReadinessOutputPath(string scanPath, ScanOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.OutputDirectory))
        {
            return Path.Combine(
                Path.GetFullPath(options.OutputDirectory),
                "upgrade-readiness-report.md");
        }

        if (!string.IsNullOrWhiteSpace(options.Output))
        {
            var outputDirectory = Path.GetDirectoryName(Path.GetFullPath(options.Output));

            if (!string.IsNullOrWhiteSpace(outputDirectory))
            {
                return Path.Combine(outputDirectory, "upgrade-readiness-report.md");
            }
        }

        return Path.Combine(scanPath, "output", "upgrade-readiness-report.md");
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
                "discovery-report.md");
        }

        return Path.Combine(scanPath, "output", "discovery-report.md");
    }
}