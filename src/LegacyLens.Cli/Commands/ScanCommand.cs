
using LegacyLens.Cli.Commands.Runners;
using LegacyLens.Cli.Progress;
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Cli.Commands;

public sealed class ScanCommand
{
    private const string DiscoveryReportFileName = "discovery-report.md";

    private readonly IScanProgressReporter _progressReporter;

    public ScanCommand()
        : this(NullScanProgressReporter.Instance)
    {
    }

    public ScanCommand(IScanProgressReporter progressReporter)
    {
        _progressReporter = progressReporter ?? NullScanProgressReporter.Instance;
    }

    public ScanResult Execute(ScanOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        var scanPath = Path.GetFullPath(options.Path);

        if (!Directory.Exists(scanPath))
        {
            throw new DirectoryNotFoundException($"Scan path does not exist: {scanPath}");
        }

        var outputPath = ResolveOutputPath(scanPath, options);

        _progressReporter.ScanStarted(scanPath, outputPath);

        _progressReporter.PhaseStarted("Discovering projects");
        var projectDiscoveryService = new ProjectDiscoveryService();
        var projects = projectDiscoveryService.DiscoverProjects(scanPath);
        _progressReporter.PhaseCompleted($"Projects discovered: {projects.Count}");
        WriteProjectVerboseDetails(projects);

        _progressReporter.PhaseStarted("Building file inventory");
        var fileInventoryBuilder = new ScanFileInventoryBuilder();
        var fileInventory = fileInventoryBuilder.Build(projects);
        _progressReporter.PhaseCompleted($"Source/config/model files indexed: {CountIndexedFiles(fileInventory)}");
        WriteFileInventoryVerboseDetails(fileInventory);

        _progressReporter.PhaseStarted("Discovering solutions");
        var solutionDiscoveryService = new SolutionDiscoveryService();
        var solutions = solutionDiscoveryService.DiscoverSolutions(scanPath);
        _progressReporter.PhaseCompleted($"Solutions discovered: {solutions.Count}");
        WriteSolutionVerboseDetails(solutions);

        _progressReporter.PhaseStarted("Scanning WCF configuration");
        var wcfConfigScanner = new WcfConfigScanner();
        var wcfEndpoints = wcfConfigScanner.Scan(scanPath);
        var wcfBehaviours = wcfConfigScanner.ScanBehaviours(scanPath);
        _progressReporter.PhaseCompleted($"WCF endpoints discovered: {wcfEndpoints.Count}");
        _progressReporter.PhaseCompleted($"WCF behaviours discovered: {wcfBehaviours.Count}");

        _progressReporter.PhaseStarted("Scanning WCF service contracts");
        var wcfServiceContractScanner = new WcfServiceContractScanner();
        var wcfServiceContracts = wcfServiceContractScanner.Scan(scanPath);
        _progressReporter.PhaseCompleted($"WCF service contracts discovered: {wcfServiceContracts.Count}");

        _progressReporter.PhaseStarted("Scanning configuration files");
        var configFileScanner = new ConfigFileScanner();
        var configFiles = configFileScanner.Scan(scanPath);
        _progressReporter.PhaseCompleted($"Configuration files discovered: {configFiles.Count}");
        WriteConfigFileVerboseDetails(configFiles);

        _progressReporter.PhaseStarted("Scanning legacy ASP.NET artifacts");
        var legacyAspNetArtifactScanner = new LegacyAspNetArtifactScanner();
        var legacyAspNetArtifacts = legacyAspNetArtifactScanner.Scan(scanPath);
        _progressReporter.PhaseCompleted($"Legacy ASP.NET artifacts discovered: {legacyAspNetArtifacts.Count}");

        _progressReporter.PhaseStarted("Analysing modernisation hints");
        var modernisationHintAnalyzer = new ModernisationHintAnalyzer();
        var modernisationHints = modernisationHintAnalyzer.Analyze(
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            wcfBehaviours,
            legacyAspNetArtifacts,
            configFiles);
        _progressReporter.PhaseCompleted($"Modernisation hints discovered: {modernisationHints.Count}");

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
            modernisationReviewAreas,
            fileInventory);

        _progressReporter.PhaseStarted("Writing discovery-report.md");
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
        _progressReporter.PhaseCompleted("discovery-report.md generated");
        _progressReporter.VerboseDetail($"Output path: {context.OutputPath}");

        var artifactResults = RunSelectedArtifacts(context);

        _progressReporter.ScanCompleted();

        var upgradeReadinessResult = FindArtifactResult(artifactResults, "upgrade-readiness");
        var upgradeBlockersResult = FindArtifactResult(artifactResults, "upgrade-blockers");
        var externalDependenciesResult = FindArtifactResult(artifactResults, "external-dependencies");
        var configurationInventoryResult = FindArtifactResult(artifactResults, "configuration-inventory");
        var dataAccessResult = FindArtifactResult(artifactResults, "data-access");
        var edmxAnalysisResult = FindArtifactResult(artifactResults, "edmx-analysis");
        var classDependenciesResult = FindArtifactResult(artifactResults, "class-dependencies");
        var solutionTopologyResult = FindArtifactResult(artifactResults, "solution-topology");

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

            ConfigurationInventoryOutputPath = configurationInventoryResult?.OutputPath,
            ConfigurationInventoryReport = configurationInventoryResult?.Report as ConfigurationInventoryReport,

            DataAccessOutputPath = dataAccessResult?.OutputPath,
            DataAccessReport = dataAccessResult?.Report as DataAccessInventoryReport,

            EdmxAnalysisOutputPath = edmxAnalysisResult?.OutputPath,
            EdmxAnalysisReport = edmxAnalysisResult?.Report as EdmxAnalysisReport,

            ClassDependenciesOutputPath = classDependenciesResult?.OutputPath,
            ClassDependenciesReport = classDependenciesResult?.Report as ClassDependencyReport,

            SolutionTopologyOutputPath = solutionTopologyResult?.OutputPath,
            SolutionTopologyReport = solutionTopologyResult?.Report as SolutionTopologyReport
        };
    }

    private static IReadOnlyList<IScanArtifactRunner> CreateArtifactRunners() =>
    [
        new UpgradeReadinessArtifactRunner(),
        new UpgradeBlockersArtifactRunner(),
        new ExternalDependenciesArtifactRunner(),
        new ConfigurationInventoryArtifactRunner(),
        new DataAccessArtifactRunner(),
        new EdmxAnalysisArtifactRunner(),
        new ClassDependenciesArtifactRunner(),
        new SolutionTopologyArtifactRunner()
    ];


    private ScanArtifactResult[] RunSelectedArtifacts(ScanContext context)
    {
        return CreateArtifactRunners()
            .Where(runner => runner.ShouldRun(context))
            .Select(runner => RunArtifact(context, runner))
            .ToArray();
    }

    private ScanArtifactResult RunArtifact(ScanContext context, IScanArtifactRunner runner)
    {
        _progressReporter.PhaseStarted($"Generating {runner.ArtifactName} artifact");
        _progressReporter.VerboseDetail($"Artifact selected: {runner.ArtifactName}");

        var result = runner.Run(context);

        _progressReporter.PhaseCompleted($"{Path.GetFileName(result.OutputPath)} generated");
        _progressReporter.VerboseDetail($"Artifact output path: {result.OutputPath}");

        return result;
    }

    private static ScanArtifactResult? FindArtifactResult(
        IEnumerable<ScanArtifactResult> artifactResults,
        string artifactName)
    {
        return artifactResults.SingleOrDefault(
            result => result.ArtifactName.Equals(
                artifactName,
                StringComparison.OrdinalIgnoreCase));
    }


    private static int CountIndexedFiles(ScanFileInventory fileInventory)
    {
        return fileInventory.CSharpFiles.Count +
               fileInventory.EdmxFiles.Count +
               fileInventory.DbmlFiles.Count +
               fileInventory.T4Files.Count +
               fileInventory.MigrationDirectories.Count;
    }

    private void WriteProjectVerboseDetails(IEnumerable<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            _progressReporter.VerboseDetail($"Project: {project.Name}");
        }
    }

    private void WriteSolutionVerboseDetails(IEnumerable<DiscoveredSolution> solutions)
    {
        foreach (var solution in solutions)
        {
            _progressReporter.VerboseDetail($"Solution: {solution.Name}");
        }
    }

    private void WriteConfigFileVerboseDetails(IEnumerable<DiscoveredConfigFile> configFiles)
    {
        foreach (var configFile in configFiles)
        {
            _progressReporter.VerboseDetail($"Configuration file: {configFile.FilePath}");
        }
    }

    private void WriteFileInventoryVerboseDetails(ScanFileInventory fileInventory)
    {
        _progressReporter.VerboseDetail($"C# files indexed: {fileInventory.CSharpFiles.Count}");
        _progressReporter.VerboseDetail($"EDMX files indexed: {fileInventory.EdmxFiles.Count}");
        _progressReporter.VerboseDetail($"DBML files indexed: {fileInventory.DbmlFiles.Count}");
        _progressReporter.VerboseDetail($"T4 files indexed: {fileInventory.T4Files.Count}");
        _progressReporter.VerboseDetail($"Migration directories indexed: {fileInventory.MigrationDirectories.Count}");
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
