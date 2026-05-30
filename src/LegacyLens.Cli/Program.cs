using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

var path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

var discoveryService = new ProjectDiscoveryService();

var projects = discoveryService.DiscoverProjects(path);
var wcfConfigScanner = new WcfConfigScanner();
var wcfEndpoints = wcfConfigScanner.Scan(path);

Console.WriteLine("Projects discovered:");

foreach (var project in projects)
{
    Console.WriteLine($"- {project.Name}");

    if (!string.IsNullOrWhiteSpace(project.TargetFramework))
    {
        Console.WriteLine($"  Target framework: {project.TargetFramework}");
    }

    foreach (var reference in project.ProjectReferences)
    {
        Console.WriteLine($"  Project reference: {reference}");
    }

    foreach (var assemblyReference in project.AssemblyReferences)
    {
        Console.WriteLine($"  Assembly reference: {assemblyReference}");
    }

    foreach (var package in project.PackageReferences)
    {
        Console.WriteLine($"  Package reference: {package}");
    }
}

Console.WriteLine();
Console.WriteLine("WCF endpoints discovered:");

if (wcfEndpoints.Count == 0)
{
    Console.WriteLine("- None");
}
else
{
    foreach (var endpoint in wcfEndpoints)
    {
        Console.WriteLine($"- {endpoint.ServiceName ?? "Unknown service"}");
        Console.WriteLine($"  Address: {endpoint.Address ?? ""}");
        Console.WriteLine($"  Binding: {endpoint.Binding ?? ""}");
        Console.WriteLine($"  Contract: {endpoint.Contract ?? ""}");
        Console.WriteLine($"  Config file: {endpoint.ConfigFilePath}");
    }
}

var wcfServiceContractScanner = new WcfServiceContractScanner();
var wcfServiceContracts = wcfServiceContractScanner.Scan(path);

Console.WriteLine();
Console.WriteLine("WCF service contracts discovered:");

if (wcfServiceContracts.Count == 0)
{
    Console.WriteLine("- None");
}
else
{
    foreach (var contract in wcfServiceContracts)
    {
        Console.WriteLine($"- {contract.Name}");
        Console.WriteLine($"  Source file: {contract.SourceFilePath}");

        foreach (var operation in contract.Operations)
        {
            Console.WriteLine($"  Operation: {operation}");
        }
    }
}

var configFileScanner = new ConfigFileScanner();
var configFiles = configFileScanner.Scan(path);

Console.WriteLine();
Console.WriteLine("Configuration files discovered:");

if (configFiles.Count == 0)
{
    Console.WriteLine("- None");
}
else
{
    foreach (var configFile in configFiles)
    {
        Console.WriteLine($"- {configFile.FilePath}");
        Console.WriteLine($"  App settings: {configFile.AppSettingsCount}");
        Console.WriteLine($"  Connection strings: {configFile.ConnectionStringsCount}");
        Console.WriteLine($"  Custom sections: {configFile.CustomSectionCount}");
    }
}

var legacyAspNetArtifactScanner = new LegacyAspNetArtifactScanner();
var legacyAspNetArtifacts = legacyAspNetArtifactScanner.Scan(path);

Console.WriteLine();
Console.WriteLine("Legacy ASP.NET artifacts discovered:");

if (legacyAspNetArtifacts.Count == 0)
{
    Console.WriteLine("- None");
}
else
{
    foreach (var artifact in legacyAspNetArtifacts)
    {
        Console.WriteLine($"- {artifact.Kind}: {artifact.Name ?? Path.GetFileName(artifact.FilePath)}");
        Console.WriteLine($"  File: {artifact.FilePath}");
    }
}

var modernisationHintAnalyzer = new ModernisationHintAnalyzer();

var modernisationHints = modernisationHintAnalyzer.Analyze(
    projects,
    wcfEndpoints,
    wcfServiceContracts,
    legacyAspNetArtifacts,
    configFiles);

Console.WriteLine();
Console.WriteLine("Modernisation hints discovered:");

if (modernisationHints.Count == 0)
{
    Console.WriteLine("- None");
}
else
{
    foreach (var hint in modernisationHints)
    {
        Console.WriteLine($"- [{hint.Severity}] {hint.Area}: {hint.Finding}");
    }
}

var solutionDiscoveryService = new SolutionDiscoveryService();
var solutions = solutionDiscoveryService.DiscoverSolutions(path);

Console.WriteLine();
Console.WriteLine("Solutions discovered:");

if (solutions.Count == 0)
{
    Console.WriteLine("- None");
}
else
{
    foreach (var solution in solutions)
    {
        Console.WriteLine($"- {solution.Name}");
        Console.WriteLine($"  Solution file: {solution.SolutionFilePath}");
        Console.WriteLine($"  Projects: {solution.ProjectFilePaths.Count}");
    }
}

Console.WriteLine();

var outputPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "output",
    "discovery-report.md");

var reportWriter = new MarkdownReportWriter();

reportWriter.Write(
    outputPath,
    solutions,
    projects,
    wcfEndpoints,
    wcfServiceContracts,
    legacyAspNetArtifacts,
    modernisationHints,
    configFiles);

Console.WriteLine();
Console.WriteLine($"Markdown report generated: {outputPath}");