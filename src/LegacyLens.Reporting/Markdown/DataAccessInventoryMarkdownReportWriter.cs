using System.Text;
using LegacyLens.Core.Analysis;

namespace LegacyLens.Reporting.Markdown;

public sealed class DataAccessInventoryMarkdownReportWriter
{
    public void Write(string outputPath, DataAccessInventoryReport report)
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

    private static string BuildMarkdown(DataAccessInventoryReport report)
    {
        var markdown = new StringBuilder();

        markdown.AppendLine("# Data Access Inventory");
        markdown.AppendLine();

        WriteSummary(markdown, report);
        WriteAnalysisScope(markdown);
        WriteOverview(markdown, report);
        WriteProjectsWithIndicators(markdown, report);
        WriteFindings(markdown, report);
        WriteCategorySection(markdown, "Connection Strings", report, DataAccessCategory.ConnectionString);
        WriteCategorySection(markdown, "Database Provider Indicators", report, DataAccessCategory.DatabaseProvider);
        WriteCategorySection(markdown, "ORM and Data Access Technologies", report,
            DataAccessCategory.EntityFramework6,
            DataAccessCategory.EntityFrameworkCore,
            DataAccessCategory.Dapper,
            DataAccessCategory.NHibernate,
            DataAccessCategory.LinqToSql);
        WriteCategorySection(markdown, "EF / EDMX Details", report, DataAccessCategory.EdmxObjectContext);
        WriteCategorySection(markdown, "ADO.NET Indicators", report, DataAccessCategory.AdoNet);
        WriteCategorySection(markdown, "Raw SQL and Stored Procedure Indicators", report,
            DataAccessCategory.RawSql,
            DataAccessCategory.StoredProcedure);
        WriteCategorySection(markdown, "Repository and Unit-of-Work Candidates", report,
            DataAccessCategory.RepositoryPattern,
            DataAccessCategory.UnitOfWorkPattern);
        WriteCategorySection(markdown, "Migration Artifacts", report, DataAccessCategory.MigrationArtifact);
        WriteSuggestedFilesToReviewFirst(markdown, report);
        WriteMigrationConsiderations(markdown, report);
        WriteSuggestedQuestions(markdown);
        WriteNotesAndLimitations(markdown);

        return markdown.ToString();
    }

    private static void WriteSummary(StringBuilder markdown, DataAccessInventoryReport report)
    {
        markdown.AppendLine("## Summary");
        markdown.AppendLine();
        markdown.AppendLine("This report is based on static source and configuration discovery. It identifies visible data access technologies, patterns, and migration concerns. A finding means “requires review”, not “verified runtime usage”.");
        markdown.AppendLine();

        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---:|");
        markdown.AppendLine($"| Data access findings | {report.Findings.Count} |");
        markdown.AppendLine($"| Categories with findings | {report.Findings.Select(x => x.Category).Distinct().Count()} |");
        markdown.AppendLine($"| Projects with findings | {report.Findings.Where(x => !string.IsNullOrWhiteSpace(x.ProjectName)).Select(x => x.ProjectName).Distinct(StringComparer.OrdinalIgnoreCase).Count()} |");
        markdown.AppendLine();
    }

    private static void WriteAnalysisScope(StringBuilder markdown)
    {
        markdown.AppendLine("## Analysis Scope");
        markdown.AppendLine();

        markdown.AppendLine("| Item | Value |");
        markdown.AppendLine("|---|---|");
        markdown.AppendLine("| Analysis mode | Static / no-build |");
        markdown.AppendLine("| Database connection attempted | No |");
        markdown.AppendLine("| SQL executed | No |");
        markdown.AppendLine("| Schema inspected | No |");
        markdown.AppendLine("| Runtime usage proven | No |");
        markdown.AppendLine("| Compatibility guarantee | No |");
        markdown.AppendLine();
    }

    private static void WriteOverview(StringBuilder markdown, DataAccessInventoryReport report)
    {
        markdown.AppendLine("## Data Access Overview");
        markdown.AppendLine();

        if (report.Findings.Count == 0)
        {
            markdown.AppendLine("No data access findings were produced by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Category | Findings |");
        markdown.AppendLine("|---|---:|");

        foreach (var group in report.Findings
                     .GroupBy(x => x.Category)
                     .OrderBy(x => GetCategoryLabel(x.Key), StringComparer.OrdinalIgnoreCase))
        {
            markdown.AppendLine($"| {Escape(GetCategoryLabel(group.Key))} | {group.Count()} |");
        }

        markdown.AppendLine();
    }

    private static void WriteProjectsWithIndicators(StringBuilder markdown, DataAccessInventoryReport report)
    {
        markdown.AppendLine("## Projects with Data Access Indicators");
        markdown.AppendLine();

        var projectGroups = report.Findings
            .Where(x => !string.IsNullOrWhiteSpace(x.ProjectName))
            .GroupBy(x => x.ProjectName!, StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (projectGroups.Length == 0)
        {
            markdown.AppendLine("No project-specific data access findings were produced.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Project | Findings | Categories |");
        markdown.AppendLine("|---|---:|---|");

        foreach (var group in projectGroups)
        {
            var categories = string.Join(
                ", ",
                group.Select(x => GetCategoryLabel(x.Category))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(x => x, StringComparer.OrdinalIgnoreCase));

            markdown.AppendLine($"| {Escape(group.Key)} | {group.Count()} | {Escape(categories)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteFindings(StringBuilder markdown, DataAccessInventoryReport report)
    {
        markdown.AppendLine("## Data Access Findings");
        markdown.AppendLine();

        if (report.Findings.Count == 0)
        {
            markdown.AppendLine("No visible data access technologies, patterns, or migration concerns were identified by the current static rules.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Category | Name | Source | Project | Evidence | Masked Value | Confidence | Migration Consideration |");
        markdown.AppendLine("|---|---|---|---|---|---|---|---|");

        foreach (var finding in report.Findings)
        {
            markdown.AppendLine(
                $"| {Escape(GetCategoryLabel(finding.Category))} " +
                $"| {Escape(finding.Name)} " +
                $"| {Escape(GetSourceLabel(finding.SourceType))}: `{Escape(finding.SourcePath)}` " +
                $"| {Escape(finding.ProjectName ?? string.Empty)} " +
                $"| {Escape(finding.Evidence)} " +
                $"| {Escape(finding.MaskedValue ?? string.Empty)} " +
                $"| {finding.Confidence} " +
                $"| {Escape(finding.MigrationConsideration)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteCategorySection(
        StringBuilder markdown,
        string heading,
        DataAccessInventoryReport report,
        params DataAccessCategory[] categories)
    {
        markdown.AppendLine($"## {heading}");
        markdown.AppendLine();

        var categorySet = categories.ToHashSet();
        var findings = report.Findings
            .Where(x => categorySet.Contains(x.Category))
            .ToArray();

        if (findings.Length == 0)
        {
            markdown.AppendLine("No findings in this category.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Category | Name | Source | Project | Evidence | Migration Consideration |");
        markdown.AppendLine("|---|---|---|---|---|---|");

        foreach (var finding in findings)
        {
            markdown.AppendLine(
                $"| {Escape(GetCategoryLabel(finding.Category))} " +
                $"| {Escape(finding.Name)} " +
                $"| {Escape(GetSourceLabel(finding.SourceType))}: `{Escape(finding.SourcePath)}` " +
                $"| {Escape(finding.ProjectName ?? string.Empty)} " +
                $"| {Escape(finding.Evidence)} " +
                $"| {Escape(finding.MigrationConsideration)} |");
        }

        markdown.AppendLine();
    }

    private static void WriteSuggestedFilesToReviewFirst(StringBuilder markdown, DataAccessInventoryReport report)
    {
        markdown.AppendLine("## Suggested Files to Review First");
        markdown.AppendLine();

        if (report.Findings.Count == 0)
        {
            markdown.AppendLine("No files were suggested because no data access findings were produced.");
            markdown.AppendLine();
            return;
        }

        markdown.AppendLine("| Priority | File / Source | Reason |");
        markdown.AppendLine("|---:|---|---|");

        var priority = 1;

        foreach (var finding in report.Findings
                     .OrderBy(x => GetReviewPriority(x.Category))
                     .ThenBy(x => x.SourcePath, StringComparer.OrdinalIgnoreCase)
                     .Take(10))
        {
            markdown.AppendLine(
                $"| {priority} " +
                $"| `{Escape(finding.SourcePath)}` " +
                $"| {Escape($"{GetCategoryLabel(finding.Category)}: {finding.Evidence}")} |");

            priority++;
        }

        markdown.AppendLine();
    }

    private static void WriteMigrationConsiderations(StringBuilder markdown, DataAccessInventoryReport report)
    {
        markdown.AppendLine("## Migration Considerations");
        markdown.AppendLine();

        var considerations = report.Findings
            .Select(x => x.MigrationConsideration)
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (considerations.Length == 0)
        {
            markdown.AppendLine("- No migration considerations were produced by the current static rules.");
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

        markdown.AppendLine("- Which databases are required for local development, test, staging, and production?");
        markdown.AppendLine("- Are connection strings environment-specific, transformed at deployment, or supplied by secret stores?");
        markdown.AppendLine("- Which ORM or data access technology is considered the source of truth for new development?");
        markdown.AppendLine("- Are stored procedures part of the application contract or owned by a separate database team?");
        markdown.AppendLine("- Are EF migrations used, or is schema deployment handled separately?");
        markdown.AppendLine("- Are repositories and unit-of-work classes still active, or are they legacy abstractions?");
        markdown.AppendLine();
    }

    private static void WriteNotesAndLimitations(StringBuilder markdown)
    {
        markdown.AppendLine("## Notes and Limitations");
        markdown.AppendLine();

        markdown.AppendLine("- This report is based on static discovery only.");
        markdown.AppendLine("- LegacyLens.NET did not connect to databases.");
        markdown.AppendLine("- LegacyLens.NET did not validate credentials or connection strings.");
        markdown.AppendLine("- LegacyLens.NET did not execute SQL.");
        markdown.AppendLine("- LegacyLens.NET did not parse or validate full SQL syntax.");
        markdown.AppendLine("- LegacyLens.NET did not inspect live database schemas.");
        markdown.AppendLine("- LegacyLens.NET did not run EF migrations or scaffold EF Core models.");
        markdown.AppendLine("- Findings should be verified by the development team before migration or refactoring decisions are made.");
        markdown.AppendLine("- Sensitive values should be masked or redacted where discovered by supported scanners.");
        markdown.AppendLine();
    }

    private static string GetCategoryLabel(DataAccessCategory category)
    {
        return category switch
        {
            DataAccessCategory.ConnectionString => "Connection String",
            DataAccessCategory.DatabaseProvider => "Database Provider",
            DataAccessCategory.EntityFramework6 => "Entity Framework 6",
            DataAccessCategory.EntityFrameworkCore => "Entity Framework Core",
            DataAccessCategory.EdmxObjectContext => "EDMX / ObjectContext",
            DataAccessCategory.AdoNet => "ADO.NET",
            DataAccessCategory.Dapper => "Dapper",
            DataAccessCategory.NHibernate => "NHibernate",
            DataAccessCategory.LinqToSql => "LINQ to SQL",
            DataAccessCategory.RawSql => "Raw SQL",
            DataAccessCategory.StoredProcedure => "Stored Procedure",
            DataAccessCategory.RepositoryPattern => "Repository Pattern",
            DataAccessCategory.UnitOfWorkPattern => "Unit of Work Pattern",
            DataAccessCategory.MigrationArtifact => "Migration Artifact",
            DataAccessCategory.UnknownRequiresReview => "Unknown / Requires Review",
            _ => category.ToString()
        };
    }

    private static string GetSourceLabel(DataAccessSourceType sourceType)
    {
        return sourceType switch
        {
            DataAccessSourceType.Configuration => "Configuration",
            DataAccessSourceType.PackageReference => "PackageReference",
            DataAccessSourceType.AssemblyReference => "AssemblyReference",
            DataAccessSourceType.ProjectFile => "ProjectFile",
            DataAccessSourceType.SourceCode => "SourceCode",
            DataAccessSourceType.EdmxFile => "EDMX",
            DataAccessSourceType.T4Template => "T4Template",
            DataAccessSourceType.DbmlFile => "DBML",
            DataAccessSourceType.MigrationFolder => "MigrationFolder",
            DataAccessSourceType.Unknown => "Unknown",
            _ => sourceType.ToString()
        };
    }

    private static int GetReviewPriority(DataAccessCategory category)
    {
        return category switch
        {
            DataAccessCategory.ConnectionString => 10,
            DataAccessCategory.DatabaseProvider => 20,
            DataAccessCategory.EntityFramework6 => 30,
            DataAccessCategory.EdmxObjectContext => 40,
            DataAccessCategory.EntityFrameworkCore => 50,
            DataAccessCategory.LinqToSql => 60,
            DataAccessCategory.RawSql => 70,
            DataAccessCategory.StoredProcedure => 80,
            DataAccessCategory.AdoNet => 90,
            DataAccessCategory.Dapper => 100,
            DataAccessCategory.NHibernate => 110,
            DataAccessCategory.RepositoryPattern => 120,
            DataAccessCategory.UnitOfWorkPattern => 130,
            DataAccessCategory.MigrationArtifact => 140,
            _ => 999
        };
    }

    private static string Escape(string? value)
    {
        return (value ?? string.Empty).Replace("|", "\\|", StringComparison.Ordinal);
    }
}