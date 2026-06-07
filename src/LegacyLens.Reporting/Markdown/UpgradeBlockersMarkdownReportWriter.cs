// src/LegacyLens.Reporting/Markdown/UpgradeBlockersMarkdownReportWriter.cs

using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class UpgradeBlockersMarkdownReportWriter
{
    public void Write(string outputPath, UpgradeBlockersReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var markdown = new StringBuilder();

        markdown.AppendLine("# Upgrade Blockers");
        markdown.AppendLine();
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static source and configuration discovery. It highlights visible blockers and migration decisions that may need review before upgrade work begins. A blocker means “requires review”, not “cannot be upgraded”.");
        markdown.AppendLine();

        markdown.AppendLine("## Target");
        markdown.AppendLine();
        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine($"| Requested upgrade target | {Escape(ValueOrGeneral(report.RequestedUpgradeTarget))} |");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| Compatibility guarantee | No |");
        markdown.AppendLine();

        WriteOverview(markdown, report);
        WriteDecisions(markdown, report);
        WriteDetails(markdown, report);
        WriteSuggestedReviewOrder(markdown);
        WriteLimitations(markdown);

        File.WriteAllText(outputPath, markdown.ToString());
    }

    private static void WriteOverview(StringBuilder markdown, UpgradeBlockersReport report)
    {
        markdown.AppendLine("## Blocker Overview");
        markdown.AppendLine();
        markdown.AppendLine("| Priority | Blocker | Impact | Evidence Count |");
        markdown.AppendLine("|---:|---|---|---:|");

        foreach (var blocker in report.Blockers)
        {
            markdown.AppendLine($"| {blocker.Priority} | {Escape(ToDisplayText(blocker.Category))} | {Escape(ToDisplayText(blocker.Impact))} | {blocker.Evidence.Count} |");
        }

        markdown.AppendLine();
    }

    private static void WriteDecisions(StringBuilder markdown, UpgradeBlockersReport report)
    {
        markdown.AppendLine("## Upgrade Blockers and Decisions");
        markdown.AppendLine();
        markdown.AppendLine("| Priority | Area | Blocker / Decision | Impact | Evidence |");
        markdown.AppendLine("|---:|---|---|---|---|");

        foreach (var blocker in report.Blockers)
        {
            var evidenceSummary = blocker.Evidence.Count == 0
                ? "No detailed evidence rows."
                : string.Join("; ", blocker.Evidence.Take(3).Select(x => x.Finding));

            markdown.AppendLine($"| {blocker.Priority} | {Escape(ToDisplayText(blocker.Category))} | {Escape(blocker.Title)} | {Escape(ToDisplayText(blocker.Impact))} | {Escape(evidenceSummary)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteDetails(StringBuilder markdown, UpgradeBlockersReport report)
    {
        markdown.AppendLine("## Blocker Details");
        markdown.AppendLine();

        foreach (var blocker in report.Blockers)
        {
            markdown.AppendLine($"### {Escape(ToDisplayText(blocker.Category))}");
            markdown.AppendLine();
            markdown.AppendLine("Why this matters:");
            markdown.AppendLine();
            markdown.AppendLine(Escape(blocker.WhyItMatters));
            markdown.AppendLine();

            markdown.AppendLine("Evidence:");
            markdown.AppendLine();
            markdown.AppendLine("| Project | File / Reference | Finding |");
            markdown.AppendLine("|---|---|---|");

            foreach (var evidence in blocker.Evidence)
            {
                markdown.AppendLine($"| {Escape(ValueOrUnknown(evidence.ProjectName))} | `{Escape(evidence.Source)}` | {Escape(evidence.Finding)} |");
            }

            markdown.AppendLine();
            markdown.AppendLine("Decision required:");
            markdown.AppendLine();

            foreach (var decision in blocker.DecisionsRequired)
            {
                markdown.AppendLine($"- {Escape(decision)}");
            }

            markdown.AppendLine();
        }
    }

    private static void WriteSuggestedReviewOrder(StringBuilder markdown)
    {
        markdown.AppendLine("## Suggested Review Order");
        markdown.AppendLine();
        markdown.AppendLine("1. Review Legacy ASP.NET / System.Web blockers first.");
        markdown.AppendLine("2. Review WCF / ServiceModel blockers and service boundary decisions.");
        markdown.AppendLine("3. Review EF6, EDMX, and data access migration decisions.");
        markdown.AppendLine("4. Review package management and package version evidence.");
        markdown.AppendLine("5. Review direct assembly references and local/vendor dependency concerns.");
        markdown.AppendLine("6. Review configuration and runtime coupling.");
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
        markdown.AppendLine("- LegacyLens.NET did not automatically migrate code.");
        markdown.AppendLine("- LegacyLens.NET did not prove that migration is impossible.");
        markdown.AppendLine("- A blocker means “requires review”, not “cannot be upgraded”.");
        markdown.AppendLine("- Findings should be verified by the development team before migration decisions are made.");
    }

    private static string ToDisplayText(UpgradeBlockerCategory category)
    {
        return category switch
        {
            UpgradeBlockerCategory.LegacyAspNetSystemWeb => "Legacy ASP.NET / System.Web",
            UpgradeBlockerCategory.WcfServiceModel => "WCF / ServiceModel",
            UpgradeBlockerCategory.Ef6EdmxDataAccess => "EF6 / EDMX / Data Access",
            UpgradeBlockerCategory.PackageManagement => "Package Management",
            UpgradeBlockerCategory.DirectAssemblyReferences => "Direct Assembly References",
            UpgradeBlockerCategory.ConfigurationRuntimeCoupling => "Configuration / Runtime Coupling",
            UpgradeBlockerCategory.WindowsOnlyPlatformSpecificApis => "Windows-only / Platform-specific APIs",
            UpgradeBlockerCategory.CustomBuildMsBuildBehaviour => "Custom Build / MSBuild Behaviour",
            UpgradeBlockerCategory.UnknownRequiresManualReview => "Unknown / Requires Manual Review",
            _ => "Unknown / Requires Manual Review"
        };
    }

    private static string ToDisplayText(UpgradeBlockerImpact impact)
    {
        return impact switch
        {
            UpgradeBlockerImpact.High => "High",
            UpgradeBlockerImpact.Medium => "Medium",
            UpgradeBlockerImpact.Low => "Low",
            UpgradeBlockerImpact.Unknown => "Unknown",
            _ => "Unknown"
        };
    }

    private static string ValueOrGeneral(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "General upgrade-blocker review" : value.Trim();

    private static string ValueOrUnknown(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "unknown" : value.Trim();

    private static string Escape(string? value) =>
        (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
}