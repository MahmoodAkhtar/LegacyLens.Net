using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Mermaid;

public sealed class ScopedClassDependencyMermaidDiagramWriter
{
    public string Write(ScopedClassDependencyReport report, int maxEdges = 40)
    {
        ArgumentNullException.ThrowIfNull(report);

        var builder = new StringBuilder();
        builder.AppendLine("```mermaid");
        builder.AppendLine("graph TD");

        if (!report.IsFound)
        {
            if (report.IsAmbiguous)
            {
                builder.AppendLine("    Ambiguous_Type[Ambiguous requested type]");
                foreach (var match in report.MatchingTypes.Take(maxEdges))
                {
                    var matchNodeId = Node($"{match.ProjectName}_{match.LineNumber}");
                    builder.AppendLine($"    Ambiguous_Type --> {matchNodeId}[{EscapeLabel(match.FullName)}]");
                }
            }
            else
            {
                builder.AppendLine($"    Requested_Type[Requested type not found: {EscapeLabel(report.RequestedTypeName)}]");
            }

            builder.AppendLine("```");
            return builder.ToString();
        }

        var root = report.RootType!;
        builder.AppendLine($"    {Node(root.Name)}[{EscapeLabel(root.Name)}]");

        var inboundEdges = CreateGroupedEdges(report.InboundDependants, report)
            .OrderByDescending(edge => edge.RelatedConcernCount)
            .ThenBy(edge => edge.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.TargetType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var outboundEdges = CreateGroupedEdges(report.OutboundDependencies, report)
            .OrderByDescending(edge => edge.RelatedConcernCount)
            .ThenBy(edge => edge.SourceType, StringComparer.OrdinalIgnoreCase)
            .ThenBy(edge => edge.TargetType, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var selectedEdges = SelectBalancedEdges(inboundEdges, outboundEdges, maxEdges);

        if (selectedEdges.Length == 0)
        {
            builder.AppendLine($"    {Node(root.Name)} --> No_Direct_Dependencies[No direct source-level dependencies discovered]");
        }
        else
        {
            foreach (var edge in selectedEdges)
            {
                builder.AppendLine(
                    $"    {Node(edge.SourceNodeKey)} -->|{EscapeLabel(string.Join(", ", edge.Labels))}| {Node(edge.TargetNodeKey)}");
            }

            if (inboundEdges.Length > selectedEdges.Count(edge => edge.Direction == ScopedEdgeDirection.Inbound))
            {
                builder.AppendLine("    MoreInboundDependants[More inbound dependants omitted from compact diagram]");
            }

            if (outboundEdges.Length > selectedEdges.Count(edge => edge.Direction == ScopedEdgeDirection.Outbound))
            {
                builder.AppendLine("    MoreOutboundDependencies[More outbound dependencies omitted from compact diagram]");
            }
        }

        builder.AppendLine("```");
        return builder.ToString();
    }

    private static ScopedEdge[] CreateGroupedEdges(
        IEnumerable<ClassDependency> dependencies,
        ScopedClassDependencyReport report)
    {
        return dependencies
            .Select(dependency => new ScopedDependencyEdge(
                dependency.SourceType,
                dependency.TargetType,
                dependency.SourceType,
                dependency.TargetType,
                dependency.Kind,
                report.InboundDependants.Contains(dependency)
                    ? ScopedEdgeDirection.Inbound
                    : ScopedEdgeDirection.Outbound))
            .GroupBy(edge => new
            {
                edge.SourceNodeKey,
                edge.TargetNodeKey,
                edge.Direction
            })
            .Select(group => new ScopedEdge(
                group.First().SourceType,
                group.First().TargetType,
                group.Key.SourceNodeKey,
                group.Key.TargetNodeKey,
                group.Key.Direction,
                group
                    .Select(edge => ToLabel(edge.Kind))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(label => label, StringComparer.OrdinalIgnoreCase)
                    .ToArray(),
                report.Concerns.Count(concern =>
                    concern.SourceType.Equals(group.First().SourceType, StringComparison.OrdinalIgnoreCase) &&
                    concern.TargetType.Equals(group.First().TargetType, StringComparison.OrdinalIgnoreCase))))
            .ToArray();
    }

    private static ScopedEdge[] SelectBalancedEdges(
        IReadOnlyList<ScopedEdge> inboundEdges,
        IReadOnlyList<ScopedEdge> outboundEdges,
        int maxEdges)
    {
        if (maxEdges <= 0)
        {
            return Array.Empty<ScopedEdge>();
        }

        if (inboundEdges.Count == 0)
        {
            return outboundEdges.Take(maxEdges).ToArray();
        }

        if (outboundEdges.Count == 0)
        {
            return inboundEdges.Take(maxEdges).ToArray();
        }

        var inboundCapacity = inboundEdges.Count <= Math.Max(1, maxEdges / 3)
            ? inboundEdges.Count
            : Math.Max(1, maxEdges / 3);

        var selectedInbound = inboundEdges.Take(inboundCapacity).ToArray();
        var remainingCapacity = Math.Max(0, maxEdges - selectedInbound.Length);
        var selectedOutbound = outboundEdges.Take(remainingCapacity).ToArray();

        return selectedInbound
            .Concat(selectedOutbound)
            .ToArray();
    }

    private sealed record ScopedDependencyEdge(
        string SourceType,
        string TargetType,
        string SourceNodeKey,
        string TargetNodeKey,
        ClassDependencyKind Kind,
        ScopedEdgeDirection Direction);

    private sealed record ScopedEdge(
        string SourceType,
        string TargetType,
        string SourceNodeKey,
        string TargetNodeKey,
        ScopedEdgeDirection Direction,
        IReadOnlyList<string> Labels,
        int RelatedConcernCount);

    private enum ScopedEdgeDirection
    {
        Inbound,
        Outbound
    }

    private static string Node(string value)
    {
        var safe = new string(value.Select(character => char.IsLetterOrDigit(character) ? character : '_').ToArray());
        return string.IsNullOrWhiteSpace(safe) ? "Unknown" : safe;
    }

    private static string EscapeLabel(string value) => value
        .Replace("|", " ", StringComparison.Ordinal)
        .Replace("\"", "'", StringComparison.Ordinal)
        .Replace("[", "(", StringComparison.Ordinal)
        .Replace("]", ")", StringComparison.Ordinal);

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
