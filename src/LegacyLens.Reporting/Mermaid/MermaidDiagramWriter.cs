using System.Text;
using LegacyLens.Core.Discovery;

namespace LegacyLens.Reporting.Mermaid;

public sealed class MermaidDiagramWriter
{
    public string BuildProjectDependencyDiagram(IReadOnlyList<DiscoveredProject> projects)
    {
        ArgumentNullException.ThrowIfNull(projects);

        var builder = new StringBuilder();

        builder.AppendLine("```mermaid");
        builder.AppendLine("graph TD");

        var projectLookup = projects.ToDictionary(
            x => Path.GetFullPath(x.ProjectFilePath),
            x => x.Name,
            StringComparer.OrdinalIgnoreCase);

        var hasReferences = false;

        foreach (var project in projects.OrderBy(x => x.Name))
        {
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);

            if (string.IsNullOrWhiteSpace(projectDirectory))
            {
                continue;
            }

            foreach (var reference in project.ProjectReferences.OrderBy(x => x))
            {
                var referencedProjectPath = Path.GetFullPath(
                    Path.Combine(projectDirectory, reference));

                var referencedProjectName = projectLookup.TryGetValue(
                    referencedProjectPath,
                    out var name)
                    ? name
                    : Path.GetFileNameWithoutExtension(reference);

                hasReferences = true;

                builder.AppendLine(
                    $"    {Sanitize(project.Name)} --> {Sanitize(referencedProjectName)}");
            }
        }

        if (!hasReferences)
        {
            builder.AppendLine("    NoProjectReferencesFound[No project references found]");
        }

        builder.AppendLine("```");

        return builder.ToString();
    }

    private static string Sanitize(string value)
    {
        return value
            .Replace(".", "_")
            .Replace("-", "_")
            .Replace(" ", "_");
    }
}