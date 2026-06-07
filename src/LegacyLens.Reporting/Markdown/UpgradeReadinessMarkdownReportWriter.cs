using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class UpgradeReadinessMarkdownReportWriter
{
    public void Write(string outputPath, UpgradeReadinessReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var markdown = new StringBuilder();

        markdown.AppendLine("# Upgrade Readiness Report");
        markdown.AppendLine();
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static source and configuration discovery. It highlights upgrade planning signals that may need review before migration. It does not prove compatibility with the requested target framework.");
        markdown.AppendLine();

        markdown.AppendLine("## Target");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Requested upgrade target | {ValueOrGeneral(report.RequestedUpgradeTarget)} |");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| Compatibility guarantee | No |");
        markdown.AppendLine();

        WriteProjectTargets(markdown, report);
        WriteOverview(markdown, report);
        WriteProjectReadiness(markdown, report);
        WriteConcerns(markdown, report);
        WritePackages(markdown, report);
        WriteAssemblies(markdown, report);
        WriteConfigurationRuntime(markdown, report);
        WriteSuggestedReviewOrder(markdown);
        WriteLimitations(markdown);

        File.WriteAllText(outputPath, markdown.ToString());
    }

    private static void WriteProjectTargets(StringBuilder markdown, UpgradeReadinessReport report)
    {
        markdown.AppendLine("## Current Project Targets");
        markdown.AppendLine();
        markdown.AppendLine("| Project | Target Framework | Project File |");
        markdown.AppendLine("|---|---|---|");

        foreach (var project in report.ProjectReadiness.OrderBy(x => x.ProjectName))
        {
            markdown.AppendLine($"| {Escape(project.ProjectName)} | {Escape(ValueOrUnknown(project.CurrentTargetFramework))} | `{Escape(project.ProjectFilePath)}` |");
        }

        markdown.AppendLine();
    }

    private static void WriteOverview(StringBuilder markdown, UpgradeReadinessReport report)
    {
        markdown.AppendLine("## Upgrade Readiness Overview");
        markdown.AppendLine();
        markdown.AppendLine("| Area | Status | Evidence |");
        markdown.AppendLine("|---|---|---|");

        foreach (var item in report.Overview)
        {
            markdown.AppendLine($"| {Escape(item.Area)} | {Escape(item.Status)} | {Escape(item.Evidence)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteProjectReadiness(StringBuilder markdown, UpgradeReadinessReport report)
    {
        markdown.AppendLine("## Project Upgrade Candidates");
        markdown.AppendLine();
        markdown.AppendLine("| Project | Current Target | Readiness | Reason |");
        markdown.AppendLine("|---|---|---|---|");

        foreach (var project in report.ProjectReadiness)
        {
            markdown.AppendLine($"| {Escape(project.ProjectName)} | {Escape(ValueOrUnknown(project.CurrentTargetFramework))} | {Escape(ToDisplayText(project.Readiness))} | {Escape(project.Reason)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteConcerns(StringBuilder markdown, UpgradeReadinessReport report)
    {
        markdown.AppendLine("## Possible Upgrade Concerns");
        markdown.AppendLine();
        markdown.AppendLine("| Concern | Evidence | Why It Matters |");
        markdown.AppendLine("|---|---|---|");

        foreach (var concern in report.Concerns)
        {
            markdown.AppendLine($"| {Escape(concern.Concern)} | {Escape(concern.Evidence)} | {Escape(concern.WhyItMatters)} |");
        }

        markdown.AppendLine();
    }

    private static void WritePackages(StringBuilder markdown, UpgradeReadinessReport report)
    {
        markdown.AppendLine("## Package Upgrade Considerations");
        markdown.AppendLine();
        markdown.AppendLine("| Project | Package | Version | Project Target | Package Target | Source Format | Source Path | Possible Concern |");
        markdown.AppendLine("|---|---|---|---|---|---|---|---|");

        foreach (var package in report.PackageConsiderations)
        {
            markdown.AppendLine($"| {Escape(package.ProjectName)} | {Escape(package.PackageName)} | {Escape(ValueOrUnknown(package.Version))} | {Escape(ValueOrUnknown(package.ProjectTargetFramework))} | {Escape(ValueOrEmpty(package.PackageTargetFramework))} | {Escape(package.SourceFormat)} | `{Escape(package.SourcePath)}` | {Escape(package.PossibleConcern)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteAssemblies(StringBuilder markdown, UpgradeReadinessReport report)
    {
        markdown.AppendLine("## Assembly Reference Considerations");
        markdown.AppendLine();
        markdown.AppendLine("| Project | Assembly | Source Project | Possible Concern |");
        markdown.AppendLine("|---|---|---|---|");

        foreach (var assembly in report.AssemblyConsiderations)
        {
            markdown.AppendLine($"| {Escape(assembly.ProjectName)} | {Escape(assembly.AssemblyName)} | `{Escape(assembly.ProjectFilePath)}` | {Escape(assembly.PossibleConcern)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteConfigurationRuntime(StringBuilder markdown, UpgradeReadinessReport report)
    {
        markdown.AppendLine("## Configuration and Runtime Considerations");
        markdown.AppendLine();
        markdown.AppendLine("| Source | Finding | Possible Upgrade Concern |");
        markdown.AppendLine("|---|---|---|");

        foreach (var item in report.ConfigurationRuntimeConsiderations)
        {
            markdown.AppendLine($"| `{Escape(item.Source)}` | {Escape(item.Finding)} | {Escape(item.PossibleConcern)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteSuggestedReviewOrder(StringBuilder markdown)
    {
        markdown.AppendLine("## Suggested Review Order");
        markdown.AppendLine();
        markdown.AppendLine("1. Review lower-risk class library candidates first.");
        markdown.AppendLine("2. Review package management style and direct package concerns.");
        markdown.AppendLine("3. Review data access projects and EF6 usage.");
        markdown.AppendLine("4. Review WCF/service boundaries.");
        markdown.AppendLine("5. Review web host, startup, routing, request pipeline, and configuration-heavy projects last.");
        markdown.AppendLine();
    }

    private static void WriteLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();
        markdown.AppendLine("- This report is based on static discovery only.");
        markdown.AppendLine("- LegacyLens.NET did not build the solution.");
        markdown.AppendLine("- LegacyLens.NET did not run the application or tests.");
        markdown.AppendLine("- LegacyLens.NET did not restore NuGet packages.");
        markdown.AppendLine("- LegacyLens.NET did not resolve transitive dependencies.");
        markdown.AppendLine("- LegacyLens.NET did not inspect NuGet package assets.");
        markdown.AppendLine("- Findings should be verified by the development team before migration decisions are made.");
    }

    private static string ToDisplayText(UpgradeReadinessLevel readiness)
    {
        return readiness switch
        {
            UpgradeReadinessLevel.LowerRiskCandidate => "Lower risk candidate",
            UpgradeReadinessLevel.ModerateReviewRequired => "Moderate review required",
            UpgradeReadinessLevel.HigherRiskReviewFirst => "Higher risk / review first",
            UpgradeReadinessLevel.Unknown => "Unknown",
            _ => "Unknown"
        };
    }

    private static string ValueOrGeneral(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "General upgrade-readiness review" : value.Trim();

    private static string ValueOrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

    private static string ValueOrEmpty(string? value) =>
        string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim();

    private static string Escape(string? value) =>
        (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
}