using System.Text;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Mermaid;

namespace LegacyLens.Reporting.Markdown;

public sealed class MarkdownReportWriter
{
    public void Write(
        string outputPath,
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));
        }

        ArgumentNullException.ThrowIfNull(projects);

        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var markdown = BuildMarkdown(projects, wcfEndpoints);

        File.WriteAllText(outputPath, markdown);
    }

    private static string BuildMarkdown(
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# LegacyLens.NET Discovery Report");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Projects discovered: {projects.Count}");
        builder.AppendLine($"- Project references discovered: {projects.Sum(x => x.ProjectReferences.Count)}");
        builder.AppendLine($"- Package references discovered: {projects.Sum(x => x.PackageReferences.Count)}");
        builder.AppendLine($"- WCF endpoints discovered: {wcfEndpoints.Count}");
        builder.AppendLine();

        AppendProjects(builder, projects);
        AppendProjectDependencyDiagram(builder, projects);
        AppendProjectReferences(builder, projects);
        AppendPackageReferences(builder, projects);
        AppendWcfEndpoints(builder, wcfEndpoints);

        return builder.ToString();
    }

    private static void AppendWcfEndpoints(StringBuilder builder, IReadOnlyList<WcfEndpoint> endpoints)
    {
        builder.AppendLine("## WCF Endpoints");
        builder.AppendLine();
        builder.AppendLine("| Service | Address | Binding | Contract | Config File |");
        builder.AppendLine("|---|---|---|---|---|");

        if (endpoints.Count == 0)
        {
            builder.AppendLine("| None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var endpoint in endpoints.OrderBy(x => x.ServiceName).ThenBy(x => x.Contract))
        {
            builder.AppendLine(
                $"| {Escape(endpoint.ServiceName ?? "Unknown")} | {Escape(endpoint.Address ?? "")} | {Escape(endpoint.Binding ?? "")} | {Escape(endpoint.Contract ?? "")} | `{endpoint.ConfigFilePath}` |");
        }

        builder.AppendLine();
    }
    
    private static void AppendProjects(StringBuilder builder, IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Projects");
        builder.AppendLine();
        builder.AppendLine("| Project | Target Framework | Project File |");
        builder.AppendLine("|---|---|---|");

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            builder.AppendLine(
                $"| {Escape(project.Name)} | {Escape(project.TargetFramework ?? "Unknown")} | `{project.ProjectFilePath}` |");
        }

        builder.AppendLine();
    }

    private static void AppendProjectDependencyDiagram(StringBuilder builder, IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Project Dependency Diagram");
        builder.AppendLine();

        var mermaidDiagramWriter = new MermaidDiagramWriter();

        builder.AppendLine(mermaidDiagramWriter.BuildProjectDependencyDiagram(projects));
        builder.AppendLine();
    }
    
    private static void AppendProjectReferences(StringBuilder builder, IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Project References");
        builder.AppendLine();
        builder.AppendLine("| From | To |");
        builder.AppendLine("|---|---|");

        var hasReferences = false;

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            foreach (var reference in project.ProjectReferences.OrderBy(x => x))
            {
                hasReferences = true;
                builder.AppendLine($"| {Escape(project.Name)} | `{reference}` |");
            }
        }

        if (!hasReferences)
        {
            builder.AppendLine("| None | None |");
        }

        builder.AppendLine();
    }

    private static void AppendPackageReferences(StringBuilder builder, IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Package References");
        builder.AppendLine();
        builder.AppendLine("| Project | Package |");
        builder.AppendLine("|---|---|");

        var hasPackages = false;

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            foreach (var package in project.PackageReferences.OrderBy(x => x))
            {
                hasPackages = true;
                builder.AppendLine($"| {Escape(project.Name)} | `{package}` |");
            }
        }

        if (!hasPackages)
        {
            builder.AppendLine("| None | None |");
        }

        builder.AppendLine();
    }

    private static string Escape(string value)
    {
        return value.Replace("|", "\\|");
    }
}