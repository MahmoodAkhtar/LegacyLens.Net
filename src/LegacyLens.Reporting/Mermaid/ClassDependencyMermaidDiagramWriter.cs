using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Mermaid;

public sealed class ClassDependencyMermaidDiagramWriter
{
    public string Write(ClassDependencyReport report, int maxEdges = 40)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("```mermaid");
        builder.AppendLine("graph TD");

        var edges = report.Dependencies
            .GroupBy(dependency => new { dependency.SourceType, dependency.TargetType })
            .Select(group => new
            {
                group.Key.SourceType,
                group.Key.TargetType,
                Labels = group
                    .Select(dependency => ToLabel(dependency.Kind))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                ConcernCount = report.Concerns.Count(concern =>
                    concern.SourceType.Equals(group.Key.SourceType, StringComparison.OrdinalIgnoreCase) &&
                    concern.TargetType.Equals(group.Key.TargetType, StringComparison.OrdinalIgnoreCase))
            })
            .OrderByDescending(edge => edge.ConcernCount)
            .ThenBy(edge => edge.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.TargetType, StringComparer.OrdinalIgnoreCase)
            .Take(maxEdges)
            .ToArray();

        if (edges.Length == 0)
        {
            builder.AppendLine("    No_Class_Dependencies_Discovered[No class dependencies discovered]");
        }
        else
        {
            foreach (var edge in edges)
            {
                builder.AppendLine($"    {Node(edge.SourceType)} -->|{EscapeLabel(string.Join(", ", edge.Labels))}| {Node(edge.TargetType)}");
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

    private static string EscapeLabel(string value) => value.Replace("|", " ", StringComparison.Ordinal).Replace("\"", "'", StringComparison.Ordinal);

    private static string ToLabel(ClassDependencyKind kind)
    {
        return kind switch
        {
            ClassDependencyKind.ConstructorParameter => "constructor parameter",
            ClassDependencyKind.Field => "field",
            ClassDependencyKind.Property => "property",
            ClassDependencyKind.MethodParameter => "method parameter",
            ClassDependencyKind.ReturnType => "return type",
            ClassDependencyKind.LocalVariable => "local variable",
            ClassDependencyKind.ObjectCreation => "hardcoded new",
            ClassDependencyKind.StaticMemberAccess => "static access",
            ClassDependencyKind.BaseClass => "inherits",
            ClassDependencyKind.InterfaceImplementation => "implements",
            ClassDependencyKind.Attribute => "attribute",
            ClassDependencyKind.GenericTypeArgument => "generic type",
            _ => kind.ToString()
        };
    }
}