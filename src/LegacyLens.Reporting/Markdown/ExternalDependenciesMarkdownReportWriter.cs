using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class ExternalDependenciesMarkdownReportWriter
{
    public void Write(string outputPath, ExternalDependenciesReport report)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(outputPath);
        ArgumentNullException.ThrowIfNull(report);

        var directory = Path.GetDirectoryName(outputPath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var markdown = BuildMarkdown(report);

        File.WriteAllText(outputPath, markdown);
    }

    private static string BuildMarkdown(ExternalDependenciesReport report)
    {
        var markdown = new StringBuilder();

        markdown.AppendLine("# External Dependencies");
        markdown.AppendLine();

        WriteSummary(markdown, report);
        WriteAnalysisScope(markdown);
        WriteDependencyOverview(markdown, report);
        WriteDependencies(markdown, report);
        WriteCategorySection(markdown, "Database Dependencies", report, ExternalDependencyCategory.Database);
        WriteCategorySection(markdown, "HTTP / Service Dependencies", report, ExternalDependencyCategory.HttpApi);
        WriteCategorySection(markdown, "WCF Dependencies", report, ExternalDependencyCategory.WcfServiceEndpoint);
        WriteCategorySection(markdown, "Messaging Dependencies", report, ExternalDependencyCategory.MessagingQueue);
        WriteCategorySection(markdown, "File System Dependencies", report, ExternalDependencyCategory.FileSystemFileShare);
        WriteCategorySection(markdown, "Email Dependencies", report, ExternalDependencyCategory.EmailSmtp);
        WriteCategorySection(markdown, "Cache / Distributed State Dependencies", report, ExternalDependencyCategory.CacheDistributedState);
        WriteCategorySection(markdown, "Authentication / Identity Provider Dependencies", report, ExternalDependencyCategory.AuthenticationIdentityProvider);
        WriteCategorySection(markdown, "Cloud Service Dependencies", report, ExternalDependencyCategory.CloudService);
        WriteBuildTimePackageFeedDependencies(markdown, report);
        WriteCategorySection(markdown, "External Assembly / Vendor DLL Dependencies", report, ExternalDependencyCategory.ExternalAssemblyVendorDll);
        WriteCategorySection(markdown, "Unknown / Requires Review Dependencies", report, ExternalDependencyCategory.UnknownRequiresReview);
        WriteSuggestedQuestions(markdown);
        WriteNotesAndLimitations(markdown);

        return markdown.ToString();
    }

    private static void WriteSummary(StringBuilder markdown, ExternalDependenciesReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();

        markdown.AppendLine("This report is based on static source and configuration discovery. It identifies possible external dependencies and the evidence found for them. A finding means “requires confirmation”, not “verified production dependency”.");
        markdown.AppendLine();

        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| Possible external dependencies | {report.Dependencies.Count} |");
        markdown.AppendLine($"| Categories with findings | {report.Dependencies.Select(x => x.Category).Distinct().Count()} |");
        markdown.AppendLine($"| Findings requiring confirmation | {report.Dependencies.Count(x => x.RequiresConfirmation)} |");
        markdown.AppendLine();
    }

    private static void WriteAnalysisScope(StringBuilder markdown)
    {
        markdown.AppendLine("## Analysis Scope");
        markdown.AppendLine();

        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| Runtime verification | No |");
        markdown.AppendLine("| External systems contacted | No |");
        markdown.AppendLine("| Credential validation | No |");
        markdown.AppendLine("| Secret values printed | No |");
        markdown.AppendLine("| Completeness guarantee | No |");
        markdown.AppendLine();
    }

    private static void WriteDependencyOverview(StringBuilder markdown, ExternalDependenciesReport report)
    {
        markdown.AppendLine("## Dependency Overview");
        markdown.AppendLine();

        if (report.Dependencies.Count == 0)
        {
            markdown.AppendLine("No possible external dependencies were identified by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Category | Count | Examples |");
        markdown.AppendLine("|---|---:|---|");

        foreach (var group in report.Dependencies
                     .GroupBy(x => x.Category)
                     .OrderBy(x => GetCategoryPriority(x.Key))
                     .ThenBy(x => GetCategoryDisplayName(x.Key), StringComparer.OrdinalIgnoreCase))
        {
            var examples = string.Join(
                ", ",
                group
                    .Select(x => x.Name)
                    .Where(x => !string.IsNullOrWhiteSpace(x))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(3));

            markdown.AppendLine($"| {Escape(GetCategoryDisplayName(group.Key))} | {group.Count()} | {Escape(examples)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteDependencies(StringBuilder markdown, ExternalDependenciesReport report)
    {
        markdown.AppendLine("## Dependencies");
        markdown.AppendLine();

        if (report.Dependencies.Count == 0)
        {
            markdown.AppendLine("No dependency findings were produced.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Category | Name / Identifier | Source | Evidence | Masked Value | Requires Confirmation |");
        markdown.AppendLine("|---|---|---|---|---|---|");

        foreach (var dependency in report.Dependencies)
        {
            markdown.AppendLine(
                $"| {Escape(GetCategoryDisplayName(dependency.Category))} " +
                $"| {Escape(dependency.Name)} " +
                $"| {Escape(GetSourceDisplayName(dependency.SourceType))} " +
                $"| {MarkdownTableCell.Evidence(dependency.Evidence)} " +
                $"| {FormatOptionalCode(dependency.MaskedValue)} " +
                $"| {FormatBoolean(dependency.RequiresConfirmation)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteCategorySection(
        StringBuilder markdown,
        string heading,
        ExternalDependenciesReport report,
        ExternalDependencyCategory category)
    {
        markdown.AppendLine($"## {heading}");
        markdown.AppendLine();

        var dependencies = report.Dependencies
            .Where(x => x.Category == category)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (dependencies.Length == 0)
        {
            markdown.AppendLine($"No {heading.ToLowerInvariant()} were identified by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Name / Identifier | Source Type | Project | Source File | Evidence | Masked Value | Confidence | Notes |");
        markdown.AppendLine("|---|---|---|---|---|---|---|---|");

        foreach (var dependency in dependencies)
        {
            markdown.AppendLine(
                $"| {Escape(dependency.Name)} " +
                $"| {Escape(GetSourceDisplayName(dependency.SourceType))} " +
                $"| {Escape(dependency.ProjectName)} " +
                $"| {FormatOptionalCode(dependency.SourcePath)} " +
                $"| {MarkdownTableCell.Evidence(dependency.Evidence)} " +
                $"| {FormatOptionalCode(dependency.MaskedValue)} " +
                $"| {Escape(dependency.Confidence.ToString())} " +
                $"| {Escape(dependency.Notes)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteBuildTimePackageFeedDependencies(
        StringBuilder markdown,
        ExternalDependenciesReport report)
    {
        markdown.AppendLine("## Build-Time / Package Feed Dependencies");
        markdown.AppendLine();

        var dependencies = report.Dependencies
            .Where(x => x.Category == ExternalDependencyCategory.PrivatePackageFeed)
            .OrderBy(x => x.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (dependencies.Length == 0)
        {
            markdown.AppendLine("No private package feed dependencies were identified by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Source | Evidence | Notes |");
        markdown.AppendLine("|---|---|---|");

        foreach (var dependency in dependencies)
        {
            markdown.AppendLine(
                $"| {FormatOptionalCode(dependency.SourcePath)} " +
                $"| {MarkdownTableCell.Evidence(dependency.Evidence)} " +
                $"| {Escape(dependency.Notes)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteSuggestedQuestions(StringBuilder markdown)
    {
        markdown.AppendLine("## Suggested Questions to Ask the Team");
        markdown.AppendLine();

        markdown.AppendLine("- Which of these dependencies are still used in production?");
        markdown.AppendLine("- Which databases are shared with other applications?");
        markdown.AppendLine("- Are any service URLs environment-specific?");
        markdown.AppendLine("- Are WCF endpoints internal only or consumed by third parties?");
        markdown.AppendLine("- Are queues, topics, or subscriptions created manually or by infrastructure automation?");
        markdown.AppendLine("- Are file shares still required?");
        markdown.AppendLine("- Where are secrets stored outside this repository?");
        markdown.AppendLine("- Which dependencies are required for local development?");
        markdown.AppendLine("- Which dependencies are required for CI builds?");
        markdown.AppendLine("- Which dependencies are required for production deployment?");
        markdown.AppendLine();
    }

    private static void WriteNotesAndLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();

        markdown.AppendLine("- This report is based on static discovery only.");
        markdown.AppendLine("- LegacyLens.NET did not run the application.");
        markdown.AppendLine("- LegacyLens.NET did not connect to any external system.");
        markdown.AppendLine("- LegacyLens.NET did not validate credentials, URLs, database servers, queues, caches, SMTP servers, file shares, cloud resources, or package feeds.");
        markdown.AppendLine("- LegacyLens.NET did not inspect production infrastructure.");
        markdown.AppendLine("- LegacyLens.NET did not prove that a dependency is active in production.");
        markdown.AppendLine("- LegacyLens.NET did not prove that a dependency is unused.");
        markdown.AppendLine("- Values that look sensitive should be masked or redacted before being written to this report.");
        markdown.AppendLine("- A dependency listed here means evidence was found, not that the dependency is confirmed active in production.");
        markdown.AppendLine("- This report is not a complete dependency map.");
        markdown.AppendLine();
    }

    private static string GetCategoryDisplayName(ExternalDependencyCategory category)
    {
        return category switch
        {
            ExternalDependencyCategory.Database => "Database",
            ExternalDependencyCategory.HttpApi => "HTTP / API",
            ExternalDependencyCategory.WcfServiceEndpoint => "WCF / Service Endpoint",
            ExternalDependencyCategory.MessagingQueue => "Messaging / Queue",
            ExternalDependencyCategory.FileSystemFileShare => "File System / File Share",
            ExternalDependencyCategory.EmailSmtp => "Email / SMTP",
            ExternalDependencyCategory.CacheDistributedState => "Cache / Distributed State",
            ExternalDependencyCategory.AuthenticationIdentityProvider => "Authentication / Identity Provider",
            ExternalDependencyCategory.CloudService => "Cloud Service",
            ExternalDependencyCategory.PrivatePackageFeed => "Private Package Feed",
            ExternalDependencyCategory.ExternalAssemblyVendorDll => "External Assembly / Vendor DLL",
            ExternalDependencyCategory.UnknownRequiresReview => "Unknown / Requires Review",
            _ => category.ToString()
        };
    }

    private static string GetSourceDisplayName(ExternalDependencySourceType sourceType)
    {
        return sourceType switch
        {
            ExternalDependencySourceType.Configuration => "Configuration",
            ExternalDependencySourceType.PackageReference => "Package Reference",
            ExternalDependencySourceType.AssemblyReference => "Assembly Reference",
            ExternalDependencySourceType.WcfEndpoint => "WCF Endpoint",
            ExternalDependencySourceType.NuGetConfig => "NuGet.config",
            ExternalDependencySourceType.SourceCode => "Source Code",
            ExternalDependencySourceType.ProjectFile => "Project File",
            ExternalDependencySourceType.Unknown => "Unknown",
            _ => sourceType.ToString()
        };
    }

    private static int GetCategoryPriority(ExternalDependencyCategory category)
    {
        return category switch
        {
            ExternalDependencyCategory.Database => 10,
            ExternalDependencyCategory.HttpApi => 20,
            ExternalDependencyCategory.WcfServiceEndpoint => 30,
            ExternalDependencyCategory.MessagingQueue => 40,
            ExternalDependencyCategory.FileSystemFileShare => 50,
            ExternalDependencyCategory.EmailSmtp => 60,
            ExternalDependencyCategory.CacheDistributedState => 70,
            ExternalDependencyCategory.AuthenticationIdentityProvider => 80,
            ExternalDependencyCategory.CloudService => 90,
            ExternalDependencyCategory.PrivatePackageFeed => 100,
            ExternalDependencyCategory.ExternalAssemblyVendorDll => 110,
            ExternalDependencyCategory.UnknownRequiresReview => 999,
            _ => 999
        };
    }

    private static string FormatBoolean(bool value)
    {
        return value ? "Yes" : "No";
    }

    private static string FormatOptionalCode(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        return $"{MarkdownTableCell.Code(value)}";
    }

    private static string Escape(string? value) => MarkdownTableCell.Escape(value);
}
