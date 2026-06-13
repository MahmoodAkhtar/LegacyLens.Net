using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Mermaid;

public sealed class SolutionTopologyMermaidDiagramWriter
{
    public string WriteSolutionProjectMap(SolutionTopologyReport report, int maxProjects = 80)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("```mermaid");
        builder.AppendLine("flowchart LR");

        var memberships = report.Memberships.Take(maxProjects).ToArray();

        if (memberships.Length == 0)
        {
            builder.AppendLine("    No_Solution_Project_Membership[No solution-project membership discovered]");
        }
        else
        {
            foreach (var membership in memberships)
            {
                builder.AppendLine($"    {Node(membership.SolutionName + "_sln")}[\"{EscapeLabel(membership.SolutionName)}.sln\"] --> {Node(membership.ProjectName)}[\"{EscapeLabel(membership.ProjectName)}\"]");
            }
        }

        builder.AppendLine("```");
        return builder.ToString();
    }

    public string WriteProjectDependencyGraph(SolutionTopologyReport report, int maxEdges = 80)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("```mermaid");
        builder.AppendLine("flowchart TD");

        var edges = report.Dependencies
            .OrderByDescending(dependency => report.Hotspots.Any(hotspot => hotspot.ProjectName.Equals(dependency.SourceProject, StringComparison.OrdinalIgnoreCase) ||
                                                                            hotspot.ProjectName.Equals(dependency.TargetProject, StringComparison.OrdinalIgnoreCase)))
            .ThenBy(dependency => dependency.SourceProject, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.TargetProject, StringComparer.OrdinalIgnoreCase)
            .Take(maxEdges)
            .ToArray();

        if (edges.Length == 0)
        {
            builder.AppendLine("    No_Project_References[No project references discovered]");
        }
        else
        {
            foreach (var dependency in edges)
            {
                builder.AppendLine($"    {Node(dependency.SourceProject)}[\"{EscapeLabel(dependency.SourceProject)}\"] --> {Node(dependency.TargetProject)}[\"{EscapeLabel(dependency.TargetProject)}\"]");
            }
        }

        builder.AppendLine("```");
        return builder.ToString();
    }

    public string WriteInferredLayerView(SolutionTopologyReport report, int maxProjects = 80)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("```mermaid");
        builder.AppendLine("flowchart TD");

        var layerOrder = new[]
        {
            "Application / Entry Points",
            "Services",
            "Domain / Core",
            "Data Access",
            "Infrastructure / Shared",
            "Contracts",
            "Tests",
            "Unknown"
        };

        var projects = report.Projects.Take(maxProjects).ToArray();

        if (projects.Length == 0)
        {
            builder.AppendLine("    No_Projects[No projects discovered]");
        }
        else
        {
            foreach (var layer in layerOrder)
            {
                var layerProjects = projects
                    .Where(project => project.Layer.Equals(layer, StringComparison.OrdinalIgnoreCase))
                    .OrderBy(project => project.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray();

                if (layerProjects.Length == 0)
                {
                    continue;
                }

                builder.AppendLine($"    subgraph {Node(layer)}[\"{EscapeLabel(layer)}\"]");

                foreach (var project in layerProjects)
                {
                    builder.AppendLine($"        {Node(project.Name)}[\"{EscapeLabel(project.Name)}\"]");
                }

                builder.AppendLine("    end");
            }

            foreach (var dependency in report.Dependencies.Take(maxProjects))
            {
                builder.AppendLine($"    {Node(dependency.SourceProject)} --> {Node(dependency.TargetProject)}");
            }
        }

        builder.AppendLine("```");
        return builder.ToString();
    }

    private static string Node(string value)
    {
        var safe = new string(value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Unknown" : safe;
    }

    private static string EscapeLabel(string value)
    {
        return value
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("\"", "'", StringComparison.Ordinal)
            .Replace("|", " ", StringComparison.Ordinal);
    }
}
