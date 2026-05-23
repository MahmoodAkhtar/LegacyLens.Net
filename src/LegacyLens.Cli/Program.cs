using LegacyLens.Core.Discovery;
using LegacyLens.Reporting.Markdown;

var path = args.Length > 0 ? args[0] : Directory.GetCurrentDirectory();

var discoveryService = new ProjectDiscoveryService();

var projects = discoveryService.DiscoverProjects(path);

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

var outputPath = Path.Combine(
    Directory.GetCurrentDirectory(),
    "output",
    "discovery-report.md");

var reportWriter = new MarkdownReportWriter();

reportWriter.Write(outputPath, projects);

Console.WriteLine();
Console.WriteLine($"Markdown report generated: {outputPath}");