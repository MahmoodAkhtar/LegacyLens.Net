using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class ConfigurationInventoryMarkdownReportWriter
{
    public void Write(string outputPath, ConfigurationInventoryReport report)
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

    private static string BuildMarkdown(ConfigurationInventoryReport report)
    {
        var markdown = new StringBuilder();

        markdown.AppendLine("# Configuration Inventory");
        markdown.AppendLine();

        WriteSummary(markdown, report);
        WriteAnalysisScope(markdown);
        WriteOverview(markdown, report);
        WriteConfigurationValuesBySourceFile(markdown, report);
        WriteSuggestedFilesToReviewFirst(markdown, report);
        WriteMigrationConsiderations(markdown, report);
        WriteSuggestedQuestions(markdown);
        WriteNotesAndLimitations(markdown);

        return markdown.ToString();
    }

    private static void WriteSummary(StringBuilder markdown, ConfigurationInventoryReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static source and configuration discovery. It identifies visible configuration files, settings, sections, transforms, and configuration API usage. A finding means “requires review”, not “verified runtime behaviour”.");
        markdown.AppendLine();

        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| Configuration findings | {report.FindingCount} |");
        markdown.AppendLine($"| Configuration files | {report.ConfigurationFileCount} |");
        markdown.AppendLine($"| Categories with findings | {report.CategoryCount} |");
        markdown.AppendLine($"| Potential migration concerns | {report.PotentialMigrationConcernCount} |");
        markdown.AppendLine();
    }

    private static void WriteAnalysisScope(StringBuilder markdown)
    {
        markdown.AppendLine("## Analysis Scope");
        markdown.AppendLine();

        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| Application run | No |");
        markdown.AppendLine("| Config transforms applied | No |");
        markdown.AppendLine("| External systems validated | No |");
        markdown.AppendLine("| Secret values printed | No |");
        markdown.AppendLine("| Runtime usage proven | No |");
        markdown.AppendLine("| Completeness guarantee | No |");
        markdown.AppendLine();
    }

    private static void WriteOverview(StringBuilder markdown, ConfigurationInventoryReport report)
    {
        markdown.AppendLine("## Configuration Overview");
        markdown.AppendLine();

        if (report.Findings.Count == 0)
        {
            markdown.AppendLine("No configuration inventory findings were identified by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Category | Findings |");
        markdown.AppendLine("|---|---:|");

        foreach (var group in report.Findings.GroupBy(finding => finding.Category).OrderBy(group => GetCategoryDisplayName(group.Key)))
        {
            markdown.AppendLine($"| {Escape(GetCategoryDisplayName(group.Key))} | {group.Count()} |");
        }

        markdown.AppendLine();
    }

    private static void WriteConfigurationValuesBySourceFile(
        StringBuilder markdown,
        ConfigurationInventoryReport report)
    {
        markdown.AppendLine("## Configuration Values by Source File");
        markdown.AppendLine();

        if (report.Findings.Count == 0)
        {
            markdown.AppendLine("No configuration findings were produced.");
            markdown.AppendLine();
            return;
        }

        var fileGroups = report.Findings
            .GroupBy(finding => finding.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                SourcePath = group.Key,
                ProjectName = FormatProjectName(group.Select(finding => finding.ProjectName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))),
                Findings = group
                    .OrderBy(finding => GetCategoryPriority(finding.Category))
                    .ThenBy(finding => GetCategoryDisplayName(finding.Category), StringComparer.OrdinalIgnoreCase)
                    .ThenBy(finding => finding.Name, StringComparer.OrdinalIgnoreCase)
                    .ToArray()
            })
            .OrderBy(group => group.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => GetSourceFileDisplayName(group.SourcePath), StringComparer.OrdinalIgnoreCase)
            .ThenBy(group => group.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        foreach (var fileGroup in fileGroups)
        {
            markdown.AppendLine($"### {Escape(fileGroup.ProjectName)} — {Escape(GetSourceFileDisplayName(fileGroup.SourcePath))}");
            markdown.AppendLine();
            markdown.AppendLine($"Source path: `{Escape(fileGroup.SourcePath)}`");
            markdown.AppendLine();

            foreach (var categoryGroup in fileGroup.Findings
                .GroupBy(finding => finding.Category)
                .OrderBy(group => GetCategoryPriority(group.Key))
                .ThenBy(group => GetCategoryDisplayName(group.Key), StringComparer.OrdinalIgnoreCase))
            {
                markdown.AppendLine($"#### {Escape(GetCategoryDisplayName(categoryGroup.Key))}");
                markdown.AppendLine();

                markdown.AppendLine("| Name | Value | Evidence | Requires Review |");
                markdown.AppendLine("|---|---|---|---|");

                foreach (var finding in categoryGroup)
                {
                    markdown.AppendLine(
                        $"| {Escape(finding.Name)} | {Escape(FormatValue(finding.MaskedValue))} | {Escape(finding.Evidence)} | {FormatBoolean(finding.RequiresReview)} |");
                }

                markdown.AppendLine();
            }
        }
    }

    private static void WriteSuggestedFilesToReviewFirst(StringBuilder markdown, ConfigurationInventoryReport report)
    {
        markdown.AppendLine("## Suggested Files to Review First");
        markdown.AppendLine();

        var files = report.Findings
            .GroupBy(finding => finding.SourcePath, StringComparer.OrdinalIgnoreCase)
            .Select(group => new
            {
                SourcePath = group.Key,
                SourceFile = GetSourceFileDisplayName(group.Key),
                ProjectName = FormatProjectName(group.Select(finding => finding.ProjectName).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value))),
                FindingCount = group.Count(),
                RequiresReviewCount = group.Count(finding => finding.RequiresReview),
                Categories = group.Select(finding => GetCategoryDisplayName(finding.Category)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(x => x).ToArray()
            })
            .OrderByDescending(file => file.RequiresReviewCount)
            .ThenByDescending(file => file.FindingCount)
            .ThenBy(file => file.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(file => file.SourceFile, StringComparer.OrdinalIgnoreCase)
            .Take(10)
            .ToArray();

        if (files.Length == 0)
        {
            markdown.AppendLine("No suggested configuration files were identified by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Source File | Findings | Requires Review | Categories | Source Path |");
        markdown.AppendLine("|---|---|---:|---:|---|---|");

        foreach (var file in files)
        {
            markdown.AppendLine(
                $"| {Escape(file.ProjectName)} | {Escape(file.SourceFile)} | {file.FindingCount} | {file.RequiresReviewCount} | {Escape(string.Join(", ", file.Categories))} | `{Escape(file.SourcePath)}` |");
        }

        markdown.AppendLine();
    }

    private static void WriteMigrationConsiderations(StringBuilder markdown, ConfigurationInventoryReport report)
    {
        markdown.AppendLine("## Migration Considerations");
        markdown.AppendLine();

        var considerations = report.Findings
            .Select(finding => finding.MigrationConsideration)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (considerations.Length == 0)
        {
            markdown.AppendLine("No migration considerations were identified by the current static rules.");
            markdown.AppendLine();
            return;
        }

        foreach (var consideration in considerations)
        {
            markdown.AppendLine($"- {consideration}");
        }

        markdown.AppendLine();
    }

    private static void WriteSuggestedQuestions(StringBuilder markdown)
    {
        markdown.AppendLine("## Suggested Questions to Ask the Team");
        markdown.AppendLine();

        markdown.AppendLine("- Which configuration files are used in each environment?");
        markdown.AppendLine("- Are Web.config or App.config transforms still part of deployment?");
        markdown.AppendLine("- Which settings are secrets and where should they be stored after migration?");
        markdown.AppendLine("- Which connection strings are required for local development, CI, test, staging, and production?");
        markdown.AppendLine("- Are custom configuration sections still actively used?");
        markdown.AppendLine("- Which authentication, authorization, logging, diagnostics, SMTP, WCF, and EF settings must be preserved?");
        markdown.AppendLine("- Are any settings injected or replaced by deployment tooling?");
        markdown.AppendLine();
    }

    private static void WriteNotesAndLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();

        markdown.AppendLine("- This report is based on static discovery only.");
        markdown.AppendLine("- LegacyLens.NET did not run the application.");
        markdown.AppendLine("- LegacyLens.NET did not apply configuration transforms.");
        markdown.AppendLine("- LegacyLens.NET did not validate credentials, tokens, certificates, connection strings, or external systems.");
        markdown.AppendLine("- LegacyLens.NET did not prove that a setting is used or unused at runtime.");
        markdown.AppendLine("- LegacyLens.NET did not fully evaluate runtime configuration inheritance or deployment-time substitutions.");
        markdown.AppendLine("- Values are shown only when a scalar value is visible to static analysis; structural findings use `N/A`.");
        markdown.AppendLine("- Sensitive values should remain masked or redacted before reports are shared.");
        markdown.AppendLine("- Findings should be verified by the development team before migration, deployment, onboarding, or environment setup decisions are made.");
        markdown.AppendLine();
    }

    private static string GetCategoryDisplayName(ConfigurationInventoryCategory category)
    {
        return category switch
        {
            ConfigurationInventoryCategory.ConfigurationFile => "Configuration File",
            ConfigurationInventoryCategory.AppSetting => "App Setting",
            ConfigurationInventoryCategory.ConnectionString => "Connection String",
            ConfigurationInventoryCategory.CustomSection => "Custom Section",
            ConfigurationInventoryCategory.EnvironmentTransform => "Environment Transform",
            ConfigurationInventoryCategory.WcfConfiguration => "WCF Configuration",
            ConfigurationInventoryCategory.AspNetIisConfiguration => "ASP.NET / IIS Configuration",
            ConfigurationInventoryCategory.BindingRedirect => "Binding Redirect",
            ConfigurationInventoryCategory.AuthenticationAuthorization => "Authentication / Authorization",
            ConfigurationInventoryCategory.LoggingDiagnostics => "Logging / Diagnostics",
            ConfigurationInventoryCategory.EntityFrameworkConfiguration => "Entity Framework Configuration",
            ConfigurationInventoryCategory.SmtpMail => "SMTP / Mail",
            ConfigurationInventoryCategory.ConfigurationApiUsage => "Configuration API Usage",
            ConfigurationInventoryCategory.JsonConfiguration => "JSON Configuration",
            ConfigurationInventoryCategory.SettingsFile => "Settings File",
            ConfigurationInventoryCategory.BuildPackageConfiguration => "Build / Package Configuration",
            ConfigurationInventoryCategory.UnknownRequiresReview => "Unknown / Requires Review",
            _ => category.ToString()
        };
    }

    private static int GetCategoryPriority(ConfigurationInventoryCategory category)
    {
        return category switch
        {
            ConfigurationInventoryCategory.ConfigurationFile => 0,
            ConfigurationInventoryCategory.ConnectionString => 1,
            ConfigurationInventoryCategory.AppSetting => 2,
            ConfigurationInventoryCategory.CustomSection => 3,
            ConfigurationInventoryCategory.EnvironmentTransform => 4,
            ConfigurationInventoryCategory.WcfConfiguration => 5,
            ConfigurationInventoryCategory.AspNetIisConfiguration => 6,
            ConfigurationInventoryCategory.BindingRedirect => 7,
            ConfigurationInventoryCategory.AuthenticationAuthorization => 8,
            ConfigurationInventoryCategory.LoggingDiagnostics => 9,
            ConfigurationInventoryCategory.EntityFrameworkConfiguration => 10,
            ConfigurationInventoryCategory.SmtpMail => 11,
            ConfigurationInventoryCategory.JsonConfiguration => 12,
            ConfigurationInventoryCategory.ConfigurationApiUsage => 13,
            ConfigurationInventoryCategory.SettingsFile => 14,
            ConfigurationInventoryCategory.BuildPackageConfiguration => 15,
            _ => 100
        };
    }

    private static string FormatProjectName(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "Unknown Project"
            : value;
    }

    private static string FormatValue(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? "N/A"
            : value;
    }

    private static string GetSourceFileDisplayName(string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return "Unknown source file";
        }

        var normalised = sourcePath.Replace('\\', '/');
        var fileName = normalised.Split('/', StringSplitOptions.RemoveEmptyEntries).LastOrDefault();

        return string.IsNullOrWhiteSpace(fileName)
            ? sourcePath
            : fileName;
    }

    private static string FormatBoolean(bool value) => value ? "Yes" : "No";

    private static string Escape(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return value.Replace("|", "\\|", StringComparison.Ordinal);
    }
}
