using System.Text;
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Mermaid;

namespace LegacyLens.Reporting.Markdown;

public sealed class MarkdownReportWriter
{
    public void Write(
        string outputPath,
        IReadOnlyList<DiscoveredSolution> solutions,
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<ModernisationHint> modernisationHints,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        if (string.IsNullOrWhiteSpace(outputPath))
        {
            throw new ArgumentException("Output path cannot be empty.", nameof(outputPath));
        }

        ArgumentNullException.ThrowIfNull(solutions);
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(wcfServiceContracts);
        ArgumentNullException.ThrowIfNull(modernisationHints);
        ArgumentNullException.ThrowIfNull(configFiles);

        var outputDirectory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(outputDirectory))
        {
            Directory.CreateDirectory(outputDirectory);
        }

        var markdown = BuildMarkdown(
            solutions,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            modernisationHints,
            configFiles);

        File.WriteAllText(outputPath, markdown);
    }

    private static string BuildMarkdown(
        IReadOnlyList<DiscoveredSolution> solutions,
        IReadOnlyList<DiscoveredProject> projects,
        IReadOnlyList<WcfEndpoint> wcfEndpoints,
        IReadOnlyList<WcfServiceContract> wcfServiceContracts,
        IReadOnlyList<ModernisationHint> modernisationHints,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        var builder = new StringBuilder();

        builder.AppendLine("# LegacyLens.NET Discovery Report");
        builder.AppendLine();

        builder.AppendLine("## Summary");
        builder.AppendLine();
        builder.AppendLine($"- Solutions discovered: {solutions.Count}");
        builder.AppendLine($"- Projects discovered: {projects.Count}");
        builder.AppendLine($"- Project references discovered: {projects.Sum(x => x.ProjectReferences.Count)}");
        builder.AppendLine($"- Package references discovered: {projects.Sum(x => x.PackageReferences.Count)}");
        builder.AppendLine($"- WCF endpoints discovered: {wcfEndpoints.Count}");
        builder.AppendLine($"- WCF service contracts discovered: {wcfServiceContracts.Count}");
        builder.AppendLine($"- Assembly references discovered: {projects.Sum(x => x.AssemblyReferences.Count)}");
        builder.AppendLine();

        AppendSolutions(builder, solutions);
        AppendProjects(builder, projects);
        AppendProjectDependencyDiagram(builder, projects);
        AppendProjectReferences(builder, projects);
        AppendAssemblyReferences(builder, projects);
        AppendPackageReferences(builder, projects);
        AppendWcfEndpoints(builder, wcfEndpoints);
        AppendWcfServiceContracts(builder, wcfServiceContracts);
        AppendConfigurationFiles(builder, configFiles);
        AppendModernisationHints(builder, modernisationHints);
        
        return builder.ToString();
    }

    private static void AppendSolutions(
        StringBuilder builder,
        IReadOnlyList<DiscoveredSolution> solutions)
    {
        builder.AppendLine("## Solutions");
        builder.AppendLine();
        builder.AppendLine("| Solution | Projects | Solution File |");
        builder.AppendLine("|---|---:|---|");

        if (solutions.Count == 0)
        {
            builder.AppendLine("| None | 0 | None |");
            builder.AppendLine();
            return;
        }

        foreach (var solution in solutions.OrderBy(x => x.Name))
        {
            builder.AppendLine(
                $"| {Escape(solution.Name)} | {solution.ProjectFilePaths.Count} | `{solution.SolutionFilePath}` |");
        }

        builder.AppendLine();
    }
    
    private static void AppendConfigurationFiles(
        StringBuilder builder,
        IReadOnlyList<DiscoveredConfigFile> configFiles)
    {
        builder.AppendLine("## Configuration Files");
        builder.AppendLine();
        builder.AppendLine("| Config File | App Settings | Connection Strings | Custom Sections |");
        builder.AppendLine("|---|---:|---:|---:|");

        if (configFiles.Count == 0)
        {
            builder.AppendLine("| None | 0 | 0 | 0 |");
            builder.AppendLine();
            return;
        }

        foreach (var configFile in configFiles.OrderBy(x => x.FilePath))
        {
            builder.AppendLine(
                $"| `{configFile.FilePath}` | {configFile.AppSettingsCount} | {configFile.ConnectionStringsCount} | {configFile.CustomSectionCount} |");
        }

        builder.AppendLine();
    }
    
    private static void AppendAssemblyReferences(StringBuilder builder, IReadOnlyList<DiscoveredProject> projects)
    {
        builder.AppendLine("## Assembly References");
        builder.AppendLine();
        builder.AppendLine("| Project | Assembly |");
        builder.AppendLine("|---|---|");

        var hasAssemblyReferences = false;

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            foreach (var assemblyReference in project.AssemblyReferences.OrderBy(x => x))
            {
                hasAssemblyReferences = true;
                builder.AppendLine($"| {Escape(project.Name)} | `{Escape(assemblyReference)}` |");
            }
        }

        if (!hasAssemblyReferences)
        {
            builder.AppendLine("| None | None |");
        }

        builder.AppendLine();
    }
    
    private static void AppendModernisationHints(
        StringBuilder builder,
        IReadOnlyList<ModernisationHint> hints)
    {
        builder.AppendLine("## Modernisation Hints");
        builder.AppendLine();
        builder.AppendLine("| Severity | Area | Finding | Reason |");
        builder.AppendLine("|---|---|---|---|");

        if (hints.Count == 0)
        {
            builder.AppendLine("| None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var hint in hints.OrderByDescending(x => x.Severity).ThenBy(x => x.Area))
        {
            builder.AppendLine(
                $"| {hint.Severity} | {Escape(hint.Area)} | {Escape(hint.Finding)} | {Escape(hint.Reason)} |");
        }

        builder.AppendLine();
    }

    private static void AppendWcfServiceContracts(
        StringBuilder builder,
        IReadOnlyList<WcfServiceContract> contracts)
    {
        builder.AppendLine("## WCF Service Contracts");
        builder.AppendLine();
        builder.AppendLine("| Contract | Operations | Source File |");
        builder.AppendLine("|---|---|---|");

        if (contracts.Count == 0)
        {
            builder.AppendLine("| None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var contract in contracts.OrderBy(x => x.Name))
        {
            var operations = contract.Operations.Count == 0
                ? ""
                : string.Join(", ", contract.Operations.Select(Escape));

            builder.AppendLine(
                $"| {Escape(contract.Name)} | {operations} | `{contract.SourceFilePath}` |");
        }

        builder.AppendLine();
    }
    
    private static void AppendWcfEndpoints(StringBuilder builder, IReadOnlyList<WcfEndpoint> endpoints)
    {
        builder.AppendLine("## WCF Endpoints");
        builder.AppendLine();
        builder.AppendLine("| Service | Address | Binding | Binding Configuration | Security Mode | Transport Credential | Message Credential | Metadata Exchange | Contract | Config File |");
        builder.AppendLine("|---|---|---|---|---|---|---|---|---|---|");

        if (endpoints.Count == 0)
        {
            builder.AppendLine("| None | None | None | None | None | None | None | None | None | None |");
            builder.AppendLine();
            return;
        }

        foreach (var endpoint in endpoints.OrderBy(x => x.ServiceName).ThenBy(x => x.Contract))
        {
            builder.AppendLine(
                $"| {Escape(endpoint.ServiceName ?? "Unknown")} | {Escape(endpoint.Address ?? "")} | {Escape(endpoint.Binding ?? "")} | {Escape(endpoint.BindingConfiguration ?? "")} | {Escape(endpoint.SecurityMode ?? "")} | {Escape(endpoint.TransportClientCredentialType ?? "")} | {Escape(endpoint.MessageClientCredentialType ?? "")} | {endpoint.IsMetadataExchangeEndpoint} | {Escape(endpoint.Contract ?? "")} | `{endpoint.ConfigFilePath}` |");
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