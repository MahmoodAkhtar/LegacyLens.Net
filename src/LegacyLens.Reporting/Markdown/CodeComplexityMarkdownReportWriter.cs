using System.Globalization;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class CodeComplexityMarkdownReportWriter
{
    public void Write(string outputPath, CodeComplexityReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);
        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(outputPath, BuildMarkdown(report));
    }

    private static string BuildMarkdown(CodeComplexityReport report)
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);

        writer.WriteLine("# Code Complexity Report");
        writer.WriteLine();
        writer.WriteLine("This report estimates cyclomatic complexity from C# syntax without building the solution. Treat the values as deterministic static discovery signals for review and refactoring prioritisation, not as official compiler, Visual Studio, runtime-risk, defect-probability, testability, maintainability, correctness, or safe-automatic-refactoring metrics.");
        writer.WriteLine();

        WriteSummary(writer, report.Summary);
        WriteProjectSummaries(writer, report.ProjectSummaries);
        WriteNamespaceSummaries(writer, report.NamespaceSummaries);
        WriteTypeHotspots(writer, report.TypeSummaries);
        WriteMemberHotspots(writer, report.Members);
        WriteGeneratedCodeNotes(writer, report.Members);
        WriteLimitations(writer);

        return writer.ToString();
    }

    private static void WriteSummary(TextWriter writer, CodeComplexityScanSummary summary)
    {
        writer.WriteLine("## Summary");
        writer.WriteLine();
        writer.WriteLine("| Metric | Value |");
        writer.WriteLine("| --- | ---: |");
        writer.WriteLine($"| C# source files analysed | {summary.SourceFileCount} |");
        writer.WriteLine($"| Members analysed | {summary.MemberCount} |");
        writer.WriteLine($"| Likely generated-code members | {summary.GeneratedMemberCount} |");
        writer.WriteLine($"| Total estimated complexity | {summary.TotalComplexity} |");
        writer.WriteLine($"| Average member complexity | {FormatNumber(summary.AverageComplexity)} |");
        writer.WriteLine($"| High-complexity members | {summary.HighComplexityMemberCount} |");
        writer.WriteLine($"| Very-high-complexity members | {summary.VeryHighComplexityMemberCount} |");
        writer.WriteLine($"| High-complexity types | {summary.HighComplexityTypeCount} |");
        writer.WriteLine($"| Very-high-complexity types | {summary.VeryHighComplexityTypeCount} |");
        writer.WriteLine();
    }

    private static void WriteProjectSummaries(TextWriter writer, IReadOnlyList<CodeComplexityProjectSummary> summaries)
    {
        writer.WriteLine("## Project Summary");
        writer.WriteLine();

        if (summaries.Count == 0)
        {
            writer.WriteLine("No C# members were discovered in indexed project-associated source files.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Project | Namespaces | Types | Members | Total Complexity | Average | Maximum Member | Highest Severity | Generated Members |");
        writer.WriteLine("| --- | ---: | ---: | ---: | ---: | ---: | ---: | --- | ---: |");

        foreach (var summary in summaries)
        {
            writer.WriteLine(
                $"| {MarkdownTableCell.Escape(summary.ProjectName)} | {summary.NamespaceCount} | {summary.TypeCount} | {summary.MemberCount} | {summary.TotalComplexity} | {FormatNumber(summary.AverageComplexity)} | {summary.MaximumMemberComplexity} | {summary.HighestSeverity} | {summary.GeneratedMemberCount} |");
        }

        writer.WriteLine();
    }

    private static void WriteNamespaceSummaries(TextWriter writer, IReadOnlyList<CodeComplexityNamespaceSummary> summaries)
    {
        writer.WriteLine("## Namespace Summary");
        writer.WriteLine();

        if (summaries.Count == 0)
        {
            writer.WriteLine("No namespace-level complexity summary is available.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Project | Namespace | Types | Members | Total Complexity | Average | Maximum Member | Highest Severity |");
        writer.WriteLine("| --- | --- | ---: | ---: | ---: | ---: | ---: | --- |");

        foreach (var summary in summaries.Take(50))
        {
            writer.WriteLine(
                $"| {MarkdownTableCell.Escape(summary.ProjectName)} | {MarkdownTableCell.Escape(summary.NamespaceName)} | {summary.TypeCount} | {summary.MemberCount} | {summary.TotalComplexity} | {FormatNumber(summary.AverageComplexity)} | {summary.MaximumMemberComplexity} | {summary.HighestSeverity} |");
        }

        writer.WriteLine();
    }

    private static void WriteTypeHotspots(TextWriter writer, IReadOnlyList<CodeComplexityTypeSummary> summaries)
    {
        writer.WriteLine("## Type Hotspots");
        writer.WriteLine();

        var hotspots = summaries
            .Where(summary => summary.HighestSeverity >= CodeComplexitySeverity.High || summary.TotalComplexity >= 20)
            .Take(50)
            .ToArray();

        if (hotspots.Length == 0)
        {
            writer.WriteLine("No high-complexity type hotspots were discovered using the current static heuristic thresholds.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Severity | Project | Type | Members | Total Complexity | Average | Maximum Member | Likely Generated | Source Path |");
        writer.WriteLine("| --- | --- | --- | ---: | ---: | ---: | ---: | --- | --- |");

        foreach (var hotspot in hotspots)
        {
            writer.WriteLine(
                $"| {hotspot.HighestSeverity} | {MarkdownTableCell.Escape(hotspot.ProjectName)} | {MarkdownTableCell.Code(hotspot.TypeName)} | {hotspot.MemberCount} | {hotspot.TotalComplexity} | {FormatNumber(hotspot.AverageComplexity)} | {hotspot.MaximumMemberComplexity} | {FormatBoolean(hotspot.ContainsLikelyGeneratedCode)} | {MarkdownTableCell.Code(hotspot.SourcePath)} |");
        }

        writer.WriteLine();
    }

    private static void WriteMemberHotspots(TextWriter writer, IReadOnlyList<CodeComplexityMember> members)
    {
        writer.WriteLine("## Member Hotspots");
        writer.WriteLine();

        var hotspots = members
            .Where(member => member.Severity >= CodeComplexitySeverity.High)
            .OrderByDescending(member => member.Complexity)
            .ThenBy(member => member.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(member => member.LineNumber)
            .Take(100)
            .ToArray();

        if (hotspots.Length == 0)
        {
            writer.WriteLine("No high or very-high complexity members were discovered using the current static heuristic thresholds.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("| Severity | Complexity | Project | Type | Member | Kind | Line | Likely Generated | Evidence | Source Path |");
        writer.WriteLine("| --- | ---: | --- | --- | --- | --- | ---: | --- | --- | --- |");

        foreach (var member in hotspots)
        {
            writer.WriteLine(
                $"| {member.Severity} | {member.Complexity} | {MarkdownTableCell.Escape(member.ProjectName)} | {MarkdownTableCell.Code(member.TypeName)} | {MarkdownTableCell.Code(member.MemberName)} | {MarkdownTableCell.Escape(member.MemberKind)} | {member.LineNumber} | {FormatBoolean(member.IsLikelyGeneratedCode)} | {MarkdownTableCell.Evidence(member.Evidence)} | {MarkdownTableCell.Code(member.SourcePath)} |");
        }

        writer.WriteLine();
    }

    private static void WriteGeneratedCodeNotes(TextWriter writer, IReadOnlyList<CodeComplexityMember> members)
    {
        var generatedMembers = members
            .Where(member => member.IsLikelyGeneratedCode)
            .Take(25)
            .ToArray();

        writer.WriteLine("## Likely Generated Code");
        writer.WriteLine();

        if (generatedMembers.Length == 0)
        {
            writer.WriteLine("No likely generated-code members were flagged by the cheap filename/content heuristics.");
            writer.WriteLine();
            return;
        }

        writer.WriteLine("Likely generated files are reported rather than silently hidden, because generated code can still affect codebase orientation and migration planning.");
        writer.WriteLine();
        writer.WriteLine("| Project | Type | Member | Complexity | Source Path |");
        writer.WriteLine("| --- | --- | --- | ---: | --- |");

        foreach (var member in generatedMembers)
        {
            writer.WriteLine(
                $"| {MarkdownTableCell.Escape(member.ProjectName)} | {MarkdownTableCell.Code(member.TypeName)} | {MarkdownTableCell.Code(member.MemberName)} | {member.Complexity} | {MarkdownTableCell.Code(member.SourcePath)} |");
        }

        writer.WriteLine();
    }

    private static void WriteLimitations(TextWriter writer)
    {
        writer.WriteLine("## Notes and Limitations");
        writer.WriteLine();
        writer.WriteLine("- Complexity is estimated from syntax-level C# constructs such as `if`, loops, switch labels, switch expression arms, `catch`, ternary expressions, logical `&&` / `||`, and pattern `when` clauses.");
        writer.WriteLine("- The analyzer does not build the solution, restore packages, create a semantic model, execute code, evaluate runtime dependency injection, or fully evaluate preprocessor symbol combinations.");
        writer.WriteLine("- Severity bands are review heuristics: Low 1-5, Moderate 6-10, High 11-20, Very High 21+.");
        writer.WriteLine("- Findings mean `review this area first`; they do not prove defects, poor maintainability, poor testability, production risk, or that automated refactoring is safe.");
        writer.WriteLine();
    }

    private static string FormatNumber(double value)
    {
        return value.ToString("0.##", CultureInfo.InvariantCulture);
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "Yes" : "No";
    }
}
