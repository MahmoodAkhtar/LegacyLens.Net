using System.Text;
using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Mermaid;

namespace LegacyLens.Reporting.Markdown;

public sealed class ScopedClassDependencyMarkdownReportWriter
{
    public void Write(string outputPath, ScopedClassDependencyReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var markdown = new StringBuilder();

        WriteHeader(markdown, report);
        WriteSummary(markdown, report);
        WriteRootType(markdown, report);
        WriteMatchState(markdown, report);

        if (report.IsFound)
        {
            WriteOutboundDependencies(markdown, report);
            WriteInboundDependants(markdown, report);
            WriteRelatedConcerns(markdown, report);
            WriteDependencyDiagram(markdown, report);
        }

        WriteNotesAndLimitations(markdown);

        File.WriteAllText(outputPath, markdown.ToString());
    }

    private static void WriteHeader(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        markdown.AppendLine("# Scoped Class Dependency Report");
        markdown.AppendLine();
        markdown.AppendLine($"Requested type: {MarkdownTableCell.Code(report.RequestedTypeName)}");
        markdown.AppendLine();
    }

    private static void WriteSummary(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is a focused static dependency view for one requested fully qualified type. It shows direct source-visible relationships only.");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Requested type | {MarkdownTableCell.Code(report.RequestedTypeName)} |");
        markdown.AppendLine($"| Generated local | {MarkdownTableCell.Code(report.GeneratedLocal.ToString("yyyy-MM-dd HH:mm:ss zzz"))} |");
        markdown.AppendLine($"| Generated UTC | {MarkdownTableCell.Code(report.GeneratedUtc.ToString("yyyy-MM-dd HH:mm:ss 'UTC'"))} |");
        markdown.AppendLine($"| C# source files analysed | {report.SourceFileCount} |");
        markdown.AppendLine($"| Types discovered | {report.DiscoveredTypeCount} |");
        markdown.AppendLine($"| Matching full-name types | {report.MatchingTypes.Count} |");
        markdown.AppendLine($"| Direct outbound dependencies | {report.OutboundDependencies.Count} |");
        markdown.AppendLine($"| Direct inbound dependants | {report.InboundDependants.Count} |");
        markdown.AppendLine($"| Related concerns | {report.Concerns.Count} |");
        markdown.AppendLine();
    }

    private static void WriteRootType(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        markdown.AppendLine("## Root Type");
        markdown.AppendLine();

        if (!report.IsFound)
        {
            markdown.AppendLine("No single root type was resolved for the requested fully qualified type name.");
            markdown.AppendLine();
            return;
        }

        var root = report.RootType!;
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Full name | {MarkdownTableCell.Code(root.FullName)} |");
        markdown.AppendLine($"| Short name | {MarkdownTableCell.Code(root.Name)} |");
        markdown.AppendLine($"| Kind | {root.Kind} |");
        markdown.AppendLine($"| Project | {Escape(root.ProjectName)} |");
        markdown.AppendLine($"| Source path | {MarkdownTableCell.Code(root.SourcePath)} |");
        markdown.AppendLine($"| Line | {root.LineNumber} |");
        markdown.AppendLine();
    }

    private static void WriteMatchState(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        if (report.IsFound)
        {
            return;
        }

        markdown.AppendLine("## Match State");
        markdown.AppendLine();

        if (report.IsAmbiguous)
        {
            markdown.AppendLine("The requested fully qualified type name matched multiple source-defined types, so LegacyLens.NET did not guess which one should be used as the root.");
            markdown.AppendLine();
            markdown.AppendLine("| Full Name | Kind | Project | Source Path | Line |");
            markdown.AppendLine("|---|---|---|---|---:|");

            foreach (var type in report.MatchingTypes)
            {
                markdown.AppendLine($"| {MarkdownTableCell.Code(type.FullName)} | {type.Kind} | {Escape(type.ProjectName)} | {MarkdownTableCell.Code(type.SourcePath)} | {type.LineNumber} |");
            }

            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("No source-defined type matched the requested fully qualified type name. LegacyLens.NET does not silently fall back to short-name matching because that could produce a misleading scoped report.");
        markdown.AppendLine();
    }

    private static void WriteOutboundDependencies(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        markdown.AppendLine("## Direct Outbound Dependencies");
        markdown.AppendLine();

        if (report.OutboundDependencies.Count == 0)
        {
            markdown.AppendLine("No direct outbound source-level dependencies were discovered for the root type.");
            markdown.AppendLine();
            return;
        }

        WriteDependencyTable(markdown, report.OutboundDependencies);
    }

    private static void WriteInboundDependants(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        markdown.AppendLine("## Direct Inbound Dependants");
        markdown.AppendLine();

        if (report.InboundDependants.Count == 0)
        {
            markdown.AppendLine("No direct inbound source-level dependants were discovered for the root type.");
            markdown.AppendLine();
            return;
        }

        WriteDependencyTable(markdown, report.InboundDependants);
    }

    private static void WriteDependencyTable(StringBuilder markdown, IReadOnlyList<ClassDependency> dependencies)
    {
        markdown.AppendLine("| Project | Source Type | Target Type | Dependency Kind | Source Path | Line | Evidence |");
        markdown.AppendLine("|---|---|---|---|---|---:|---|");

        foreach (var dependency in dependencies)
        {
            markdown.AppendLine($"| {Escape(dependency.ProjectName)} | {MarkdownTableCell.Code(dependency.SourceType)} | {MarkdownTableCell.Code(dependency.TargetType)} | {Escape(ToLabel(dependency.Kind))} | {MarkdownTableCell.Code(dependency.SourcePath)} | {dependency.LineNumber} | {MarkdownTableCell.Code(dependency.Evidence)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteRelatedConcerns(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        markdown.AppendLine("## Related Review Concerns");
        markdown.AppendLine();

        if (report.Concerns.Count == 0)
        {
            markdown.AppendLine("No coupling concerns involving the root type were discovered by the MVP rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Severity | Source Type | Target Type | Dependency Kind | Evidence | Why It Matters | Recommendation |");
        markdown.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var concern in report.Concerns)
        {
            markdown.AppendLine($"| {concern.Severity} | {MarkdownTableCell.Code(concern.SourceType)} | {MarkdownTableCell.Code(concern.TargetType)} | {Escape(ToLabel(concern.DependencyKind))} | {MarkdownTableCell.Code(concern.Evidence)} | {Escape(concern.WhyItMatters)} | {Escape(concern.Recommendation)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteDependencyDiagram(StringBuilder markdown, ScopedClassDependencyReport report)
    {
        markdown.AppendLine("## Dependency Diagram");
        markdown.AppendLine();
        markdown.AppendLine(new ScopedClassDependencyMermaidDiagramWriter().Write(report));
    }

    private static void WriteNotesAndLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();
        markdown.AppendLine("- This report is based on static C# source inspection only.");
        markdown.AppendLine("- The scoped root is resolved only by the requested fully qualified type name, case-insensitively.");
        markdown.AppendLine("- Dependencies are direct source-visible relationships found by the existing no-build class dependency analysis.");
        markdown.AppendLine("- Runtime dependency injection, reflection, dynamic loading, generated-code behaviour, transitive dependencies, conditional runtime behaviour, and runtime call graphs are not resolved.");
        markdown.AppendLine("- Findings mean source-level dependency evidence was found; they do not prove runtime usage.");
    }

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

    private static string Escape(string? value) => MarkdownTableCell.Escape(value);
}
