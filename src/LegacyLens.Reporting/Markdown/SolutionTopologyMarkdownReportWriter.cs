using System.Text;
using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Mermaid;

namespace LegacyLens.Reporting.Markdown;

public sealed class SolutionTopologyMarkdownReportWriter
{
    public void Write(string outputPath, SolutionTopologyReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var markdown = new StringBuilder();

        WriteHeader(markdown);
        WriteSummary(markdown, report);
        WriteAnalysisScope(markdown);
        WriteSolutions(markdown, report);
        WriteProjectsBySolution(markdown, report);
        WriteSharedProjects(markdown, report);
        WriteProjectRoles(markdown, report);
        WriteEntryPoints(markdown, report);
        WriteTestProjects(markdown, report);
        WriteProjectDependencyGraph(markdown, report);
        WriteSolutionProjectMap(markdown, report);
        WriteLayerView(markdown, report);
        WriteHotspots(markdown, report);
        WriteCycles(markdown, report);
        WriteReadingOrder(markdown, report);
        WriteDependencyInventory(markdown, report);
        WriteNotes(markdown);

        File.WriteAllText(outputPath, markdown.ToString());
    }

    private static void WriteHeader(StringBuilder markdown)
    {
        markdown.AppendLine("# Solution Topology");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static solution, project, source, and configuration evidence. It is intended to help a developer understand solution/project structure and decide where to start reading. Findings should be verified by the development team.");
        markdown.AppendLine();
    }

    private static void WriteSummary(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Count |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| Solutions discovered | {report.Summary.SolutionCount} |");
        markdown.AppendLine($"| Projects discovered | {report.Summary.ProjectCount} |");
        markdown.AppendLine($"| Solution-project memberships discovered | {report.Summary.SolutionProjectMembershipCount} |");
        markdown.AppendLine($"| Projects shared across multiple solutions | {report.Summary.SharedProjectCount} |");
        markdown.AppendLine($"| Project references discovered | {report.Summary.ProjectReferenceCount} |");
        markdown.AppendLine($"| Possible entry-point projects | {report.Summary.PossibleEntryPointProjectCount} |");
        markdown.AppendLine($"| Possible test projects | {report.Summary.PossibleTestProjectCount} |");
        markdown.AppendLine($"| Possible circular project references | {report.Summary.PossibleCircularProjectReferenceCount} |");
        markdown.AppendLine($"| Dependency hotspots | {report.Summary.DependencyHotspotCount} |");
        markdown.AppendLine();
    }

    private static void WriteAnalysisScope(StringBuilder markdown)
    {
        markdown.AppendLine("## Analysis Scope");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| Solution files parsed | Yes |");
        markdown.AppendLine("| Project files parsed | Yes |");
        markdown.AppendLine("| Runtime dependency validation | No |");
        markdown.AppendLine("| Build required | No |");
        markdown.AppendLine("| Completeness guarantee | No |");
        markdown.AppendLine();
    }

    private static void WriteSolutions(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Solutions");
        markdown.AppendLine();

        if (report.Solutions.Count == 0)
        {
            markdown.AppendLine("No solution files were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Solution | Projects | Possible Entry Points | Possible Test Projects | Solution File | Notes |");
        markdown.AppendLine("|---|---:|---|---|---|---|");

        foreach (var solution in report.Solutions)
        {
            markdown.AppendLine($"| {Escape(solution.Name)} | {solution.ProjectCount} | {Join(solution.PossibleEntryPointProjects)} | {Join(solution.PossibleTestProjects)} | {MarkdownTableCell.Code(solution.SolutionFilePath)} | {Escape(solution.Notes)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteProjectsBySolution(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Projects by Solution");
        markdown.AppendLine();

        if (report.Memberships.Count == 0)
        {
            markdown.AppendLine("No solution-project memberships were discovered.");
            markdown.AppendLine();
            return;
        }

        var projectLookup = report.Projects.ToDictionary(project => project.ProjectFilePath, StringComparer.OrdinalIgnoreCase);

        foreach (var group in report.Memberships.GroupBy(membership => membership.SolutionName).OrderBy(group => group.Key, StringComparer.OrdinalIgnoreCase))
        {
            markdown.AppendLine($"### {Escape(group.Key)}");
            markdown.AppendLine();
            markdown.AppendLine("| Project | Target Framework | Inferred Role | Outgoing References | Incoming References | Packages | Assembly References | Project File |");
            markdown.AppendLine("|---|---|---|---:|---:|---:|---:|---|");

            foreach (var membership in group.OrderBy(x => x.ProjectName, StringComparer.OrdinalIgnoreCase))
            {
                projectLookup.TryGetValue(membership.ProjectFilePath, out var project);
                markdown.AppendLine($"| {Escape(membership.ProjectName)} | {Escape(project?.TargetFramework ?? "Unknown")} | {Escape(ToRoleLabel(project?.Role.Role ?? ProjectTopologyRole.Unknown))} | {project?.OutgoingProjectReferenceCount ?? 0} | {project?.IncomingProjectReferenceCount ?? 0} | {project?.PackageReferenceCount ?? 0} | {project?.AssemblyReferenceCount ?? 0} | {MarkdownTableCell.Code(membership.ProjectFilePath)} |");
            }

            markdown.AppendLine();
        }
    }

    private static void WriteSharedProjects(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Projects Shared Across Solutions");
        markdown.AppendLine();

        if (report.SharedProjects.Count == 0)
        {
            markdown.AppendLine("No projects were found in more than one solution file.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Solutions | Count | Why It Matters | Project File |");
        markdown.AppendLine("|---|---|---:|---|---|");

        foreach (var project in report.SharedProjects)
        {
            markdown.AppendLine($"| {Escape(project.ProjectName)} | {Join(project.SolutionNames)} | {project.SolutionCount} | {Escape(project.WhyItMatters)} | {MarkdownTableCell.Code(project.ProjectFilePath)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteProjectRoles(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Inferred Project Roles");
        markdown.AppendLine();
        markdown.AppendLine("Role classification is heuristic and evidence-backed. `Unknown` means no strong static signal was found.");
        markdown.AppendLine();

        if (report.Projects.Count == 0)
        {
            markdown.AppendLine("No projects were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Inferred Role | Confidence | Layer | Evidence |");
        markdown.AppendLine("|---|---|---|---|---|");

        foreach (var project in report.Projects)
        {
            markdown.AppendLine($"| {Escape(project.Name)} | {Escape(ToRoleLabel(project.Role.Role))} | {project.Role.Confidence} | {Escape(project.Layer)} | {MarkdownTableCell.Evidence(FirstEvidence(project.Role.Evidence))} |");
        }

        markdown.AppendLine();
    }

    private static void WriteEntryPoints(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Possible Entry Points");
        markdown.AppendLine();

        var entryPoints = report.Projects.Where(project => project.IsPossibleEntryPoint).ToArray();
        if (entryPoints.Length == 0)
        {
            markdown.AppendLine("No possible application entry-point projects were discovered from static evidence.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Inferred Entry-Point Type | Confidence | Evidence | Suggested Review |");
        markdown.AppendLine("|---|---|---|---|---|");

        foreach (var project in entryPoints)
        {
            markdown.AppendLine($"| {Escape(project.Name)} | {Escape(project.PossibleEntryPointType ?? "Unknown application entry point")} | {project.EntryPointConfidence} | {MarkdownTableCell.Evidence(FirstEvidence(project.EntryPointEvidence))} | Review startup, hosting, routing, configuration, and project references first. |");
        }

        markdown.AppendLine();
    }

    private static void WriteTestProjects(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Possible Test Projects");
        markdown.AppendLine();

        var testProjects = report.Projects.Where(project => project.IsPossibleTestProject).ToArray();
        if (testProjects.Length == 0)
        {
            markdown.AppendLine("No possible test projects were discovered from static evidence.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Confidence | Evidence |");
        markdown.AppendLine("|---|---|---|");

        foreach (var project in testProjects)
        {
            markdown.AppendLine($"| {Escape(project.Name)} | {project.TestProjectConfidence} | {MarkdownTableCell.Evidence(FirstEvidence(project.TestProjectEvidence))} |");
        }

        markdown.AppendLine();
    }

    private static void WriteProjectDependencyGraph(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Project Dependency Graph");
        markdown.AppendLine();
        markdown.AppendLine(new SolutionTopologyMermaidDiagramWriter().WriteProjectDependencyGraph(report));
    }

    private static void WriteSolutionProjectMap(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Solution-to-Project Map");
        markdown.AppendLine();
        markdown.AppendLine(new SolutionTopologyMermaidDiagramWriter().WriteSolutionProjectMap(report));
    }

    private static void WriteLayerView(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Inferred Layer View");
        markdown.AppendLine();
        markdown.AppendLine("This layer view is inferred from static evidence and project naming. It should not be treated as proven architecture.");
        markdown.AppendLine();
        markdown.AppendLine(new SolutionTopologyMermaidDiagramWriter().WriteInferredLayerView(report));
    }

    private static void WriteHotspots(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Dependency Hotspots");
        markdown.AppendLine();
        markdown.AppendLine("A hotspot means `review first`; it does not necessarily mean bad design.");
        markdown.AppendLine();

        if (report.Hotspots.Count == 0)
        {
            markdown.AppendLine("No dependency hotspots were discovered by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Incoming References | Outgoing References | Solution Count | Possible Role | Suggested Review |");
        markdown.AppendLine("|---|---:|---:|---:|---|---|");

        foreach (var hotspot in report.Hotspots)
        {
            markdown.AppendLine($"| {Escape(hotspot.ProjectName)} | {hotspot.IncomingReferences} | {hotspot.OutgoingReferences} | {hotspot.SolutionCount} | {Escape(hotspot.PossibleRole)} | {Escape(hotspot.SuggestedReview)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteCycles(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Possible Circular Project References");
        markdown.AppendLine();

        if (report.PossibleCircularDependencies.Count == 0)
        {
            markdown.AppendLine("No possible circular project references were discovered from static project reference evidence.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Cycle | Projects Involved | Evidence | Suggested Review |");
        markdown.AppendLine("|---|---|---|---|");

        foreach (var cycle in report.PossibleCircularDependencies)
        {
            markdown.AppendLine($"| {Escape(cycle.Cycle)} | {Join(cycle.ProjectsInvolved)} | {MarkdownTableCell.Evidence(cycle.Evidence)} | {Escape(cycle.SuggestedReview)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteReadingOrder(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Suggested Reading Order");
        markdown.AppendLine();

        if (report.SuggestedReadingOrder.Count == 0)
        {
            markdown.AppendLine("No suggested reading order could be produced because no projects were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Order | Project | Reason | Evidence |");
        markdown.AppendLine("|---:|---|---|---|");

        foreach (var step in report.SuggestedReadingOrder)
        {
            markdown.AppendLine($"| {step.Order} | {Escape(step.ProjectName)} | {Escape(step.Reason)} | {MarkdownTableCell.Evidence(step.Evidence)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteDependencyInventory(StringBuilder markdown, SolutionTopologyReport report)
    {
        markdown.AppendLine("## Full Project Dependency Inventory");
        markdown.AppendLine();

        if (report.Dependencies.Count == 0)
        {
            markdown.AppendLine("No project-to-project references were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Source Project | Target Project | Source Solution(s) | Source Project File | Target Project File | Evidence |");
        markdown.AppendLine("|---|---|---|---|---|---|");

        foreach (var dependency in report.Dependencies)
        {
            markdown.AppendLine($"| {Escape(dependency.SourceProject)} | {Escape(dependency.TargetProject)} | {Join(dependency.SourceSolutionNames)} | {MarkdownTableCell.Code(dependency.SourceProjectFilePath)} | {MarkdownTableCell.Code(dependency.TargetProjectFilePath)} | {MarkdownTableCell.Code(dependency.Evidence)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteNotes(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();
        markdown.AppendLine("- This report is based on static source, solution, project, and configuration discovery only.");
        markdown.AppendLine("- LegacyLens.NET did not build the solution or restore NuGet packages.");
        markdown.AppendLine("- Entry-point, test-project, project-role, and layer classifications are inferred from visible evidence and naming patterns.");
        markdown.AppendLine("- Project reference evidence does not prove runtime execution, runtime dependency injection behaviour, or complete architecture.");
        markdown.AppendLine("- A hotspot means `review first`; it does not necessarily mean bad design.");
        markdown.AppendLine("- Findings should be verified by the development team before migration, refactoring, or ownership decisions are made.");
        markdown.AppendLine();
    }

    private static string FirstEvidence(IReadOnlyList<ProjectRoleEvidence> evidence)
    {
        return evidence.Count == 0 ? "No strong static evidence found." : $"{evidence[0].Evidence} ({evidence[0].SourcePath})";
    }

    private static string Join(IEnumerable<string> values)
    {
        var materialised = values.Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        return materialised.Length == 0 ? "None" : Escape(string.Join(", ", materialised));
    }

    private static string ToRoleLabel(ProjectTopologyRole role)
    {
        return role switch
        {
            ProjectTopologyRole.WebApplication => "Web application",
            ProjectTopologyRole.ApiApplication => "API application",
            ProjectTopologyRole.ConsoleApplication => "Console application",
            ProjectTopologyRole.WindowsService => "Windows service",
            ProjectTopologyRole.WcfServiceHost => "WCF service host",
            ProjectTopologyRole.WorkerServiceHost => "Worker/service host",
            ProjectTopologyRole.ServiceLibrary => "Service library",
            ProjectTopologyRole.DomainCore => "Domain/Core",
            ProjectTopologyRole.DataAccess => "Data access",
            ProjectTopologyRole.Infrastructure => "Infrastructure",
            ProjectTopologyRole.SharedCommon => "Shared/Common",
            ProjectTopologyRole.Contracts => "Contracts",
            ProjectTopologyRole.Test => "Test",
            _ => "Unknown"
        };
    }

    private static string Escape(string? value) => MarkdownTableCell.Escape(value);
}
