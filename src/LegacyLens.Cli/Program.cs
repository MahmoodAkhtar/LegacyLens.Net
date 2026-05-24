using LegacyLens.Core.Analysis;
using LegacyLens.Core.Discovery;
using LegacyLens.Reporting.Markdown;
using LegacyLens.Core.Wcf;

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

var modernisationHintAnalyzer = new ModernisationHintAnalyzer();

var modernisationHints = modernisationHintAnalyzer.Analyze(
    projects,
    wcfEndpoints,
    wcfServiceContracts);

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

var outputPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "output",
    "discovery-report.md");

var reportWriter = new MarkdownReportWriter();

reportWriter.Write(
    outputPath, 
    projects, 
    wcfEndpoints, 
    wcfServiceContracts, 
    modernisationHints);

Console.WriteLine();
Console.WriteLine($"Markdown report generated: {outputPath}");

