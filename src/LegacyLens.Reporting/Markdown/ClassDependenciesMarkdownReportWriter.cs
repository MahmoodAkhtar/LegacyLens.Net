using System.Text;
using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Mermaid;

namespace LegacyLens.Reporting.Markdown;

public sealed class ClassDependenciesMarkdownReportWriter
{
    public void Write(string outputPath, ClassDependencyReport report)
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
        WriteTopCoupledTypes(markdown, report);
        WriteCouplingConcerns(markdown, report);
        WriteHardcodedConcreteDependencies(markdown, report);
        WriteStaticDependencyHotspots(markdown, report);
        WriteDependencyDiagram(markdown, report);
        WriteTypeDependencyInventory(markdown, report);
        WriteTypeDetails(markdown, report);
        WriteNotesAndLimitations(markdown);

        File.WriteAllText(outputPath, markdown.ToString());
    }

    private static void WriteHeader(StringBuilder markdown)
    {
        markdown.AppendLine("# Class Dependency Report");
        markdown.AppendLine();
    }

    private static void WriteSummary(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static C# source inspection. It highlights source-level dependencies and possible coupling concerns. It does not prove runtime usage or produce a runtime call graph.");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Count |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| Projects analysed | {report.Types.Select(type => type.ProjectName).Distinct(StringComparer.OrdinalIgnoreCase).Count()} |");
        markdown.AppendLine($"| C# source files analysed | {report.SourceFileCount} |");
        markdown.AppendLine($"| Types discovered | {report.Types.Count} |");
        markdown.AppendLine($"| Dependency relationships discovered | {report.Dependencies.Count} |");
        markdown.AppendLine($"| Coupling concerns discovered | {report.Concerns.Count} |");
        markdown.AppendLine($"| Hardcoded concrete dependencies discovered | {report.HardcodedConcreteDependencyCount} |");
        markdown.AppendLine($"| Static dependencies discovered | {report.StaticDependencyCount} |");
        markdown.AppendLine($"| High-coupling types discovered | {report.Hotspots.Count} |");
        markdown.AppendLine();
    }

    private static void WriteAnalysisScope(StringBuilder markdown)
    {
        markdown.AppendLine("## Analysis Scope");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| MSBuild compilation performed | No |");
        markdown.AppendLine("| NuGet restore performed | No |");
        markdown.AppendLine("| Runtime dependency injection resolved | No |");
        markdown.AppendLine("| Runtime call graph produced | No |");
        markdown.AppendLine("| Compatibility guarantee | No |");
        markdown.AppendLine();
    }

    private static void WriteTopCoupledTypes(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Top Coupled Types");
        markdown.AppendLine();

        if (report.Hotspots.Count == 0)
        {
            markdown.AppendLine("No high-coupling type hotspots were discovered by the MVP rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Type | Project | Outgoing Dependencies | Incoming Dependencies | Concern Count | Notes |");
        markdown.AppendLine("|---|---|---:|---:|---:|---|");

        foreach (var hotspot in report.Hotspots)
        {
            markdown.AppendLine($"| `{Escape(hotspot.Type)}` | {Escape(hotspot.ProjectName)} | {hotspot.OutgoingDependencyCount} | {hotspot.IncomingDependencyCount} | {hotspot.ConcernCount} | {Escape(hotspot.Notes)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteCouplingConcerns(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Coupling Concerns");
        markdown.AppendLine();

        if (report.Concerns.Count == 0)
        {
            markdown.AppendLine("No coupling concerns were discovered by the MVP rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Severity | Source Type | Target Type | Dependency Kind | Evidence | Why It Matters | Recommendation |");
        markdown.AppendLine("|---|---|---|---|---|---|---|");

        foreach (var concern in report.Concerns)
        {
            markdown.AppendLine($"| {concern.Severity} | `{Escape(concern.SourceType)}` | `{Escape(concern.TargetType)}` | {Escape(ToLabel(concern.DependencyKind))} | `{Escape(concern.Evidence)}` | {Escape(concern.WhyItMatters)} | {Escape(concern.Recommendation)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteHardcodedConcreteDependencies(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Hardcoded Concrete Dependencies");
        markdown.AppendLine();

        var rows = report.Concerns
            .Where(concern => concern.DependencyKind == ClassDependencyKind.ObjectCreation)
            .ToArray();

        if (rows.Length == 0)
        {
            markdown.AppendLine("No hardcoded concrete dependencies were discovered by the MVP rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Source Type | Target Type | Project | Evidence | Severity | Suggested Review |");
        markdown.AppendLine("|---|---|---|---|---|---|");

        foreach (var row in rows)
        {
            markdown.AppendLine($"| `{Escape(row.SourceType)}` | `{Escape(row.TargetType)}` | {Escape(row.ProjectName)} | `{Escape(row.Evidence)}` | {row.Severity} | {Escape(row.Recommendation)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteStaticDependencyHotspots(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Static Dependency Hotspots");
        markdown.AppendLine();

        var rows = report.Concerns
            .Where(concern => concern.DependencyKind == ClassDependencyKind.StaticMemberAccess)
            .ToArray();

        if (rows.Length == 0)
        {
            markdown.AppendLine("No static dependency hotspots were discovered by the MVP rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Source Type | Static Dependency | Project | Evidence | Severity | Suggested Review |");
        markdown.AppendLine("|---|---|---|---|---|---|");

        foreach (var row in rows)
        {
            markdown.AppendLine($"| `{Escape(row.SourceType)}` | `{Escape(row.TargetType)}` | {Escape(row.ProjectName)} | `{Escape(row.Evidence)}` | {row.Severity} | {Escape(row.Recommendation)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteDependencyDiagram(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Dependency Diagram");
        markdown.AppendLine();
        markdown.AppendLine(new ClassDependencyMermaidDiagramWriter().Write(report));
    }

    private static void WriteTypeDependencyInventory(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Type Dependency Inventory");
        markdown.AppendLine();

        if (report.Dependencies.Count == 0)
        {
            markdown.AppendLine("No source-level type dependencies were discovered.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Source Type | Target Type | Dependency Kind | Source Path | Line | Evidence |");
        markdown.AppendLine("|---|---|---|---|---|---:|---|");

        foreach (var dependency in report.Dependencies)
        {
            markdown.AppendLine($"| {Escape(dependency.ProjectName)} | `{Escape(dependency.SourceType)}` | `{Escape(dependency.TargetType)}` | {Escape(ToLabel(dependency.Kind))} | `{Escape(dependency.SourcePath)}` | {dependency.LineNumber} | `{Escape(dependency.Evidence)}` |");
        }

        markdown.AppendLine();
    }

    private static void WriteTypeDetails(StringBuilder markdown, ClassDependencyReport report)
    {
        markdown.AppendLine("## Type Details");
        markdown.AppendLine();

        if (report.Types.Count == 0)
        {
            markdown.AppendLine("No source-defined types were discovered.");
            markdown.AppendLine();
            return;
        }

        foreach (var type in report.Types.OrderBy(type => type.ProjectName, StringComparer.OrdinalIgnoreCase).ThenBy(type => type.Name, StringComparer.OrdinalIgnoreCase))
        {
            var dependencies = report.Dependencies
                .Where(dependency => dependency.SourceType.Equals(type.Name, StringComparison.OrdinalIgnoreCase))
                .ToArray();

            markdown.AppendLine($"### {Escape(type.Name)}");
            markdown.AppendLine();
            markdown.AppendLine($"- Project: {Escape(type.ProjectName)}");
            markdown.AppendLine($"- Kind: {type.Kind}");
            markdown.AppendLine($"- Source: `{Escape(type.SourcePath)}`");
            markdown.AppendLine($"- Dependencies: {dependencies.Length}");
            markdown.AppendLine();
        }
    }

    private static void WriteNotesAndLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();
        markdown.AppendLine("- This report is based on static source inspection only.");
        markdown.AppendLine("- LegacyLens.NET did not build the solution or restore NuGet packages.");
        markdown.AppendLine("- Findings mean source-level dependency evidence was found; they do not prove runtime usage.");
        markdown.AppendLine("- Runtime dependency injection, reflection, dynamic loading, generated-code behaviour, and conditional runtime behaviour are not resolved.");
        markdown.AppendLine("- The Mermaid diagram is intentionally focused and may omit low-priority edges when many dependencies are discovered.");
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

    private static string Escape(string? value) => (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal).Replace("\r", " ", StringComparison.Ordinal).Replace("\n", " ", StringComparison.Ordinal);
}