using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class ConfigurationInventoryMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public ConfigurationInventoryMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ConfigurationInventoryMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new ConfigurationInventoryMarkdownReportWriter();

        var report = new ConfigurationInventoryReport(Array.Empty<ConfigurationInventoryFinding>());

        Assert.Throws<ArgumentException>(() => writer.Write("", report));
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new ConfigurationInventoryMarkdownReportWriter();

        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WhenDirectoryDoesNotExist_CreatesDirectoryAndWritesReport()
    {
        var outputDirectory = Path.Combine(_tempDirectory, "reports", "configuration");
        var outputPath = Path.Combine(outputDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(Array.Empty<ConfigurationInventoryFinding>());

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        Assert.True(Directory.Exists(outputDirectory));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Write_WhenReportIsEmpty_WritesExpectedHeadingsAndNoFindingsText()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(Array.Empty<ConfigurationInventoryFinding>());

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Configuration Inventory", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Analysis Scope", markdown);
        Assert.Contains("## Configuration Overview", markdown);
        Assert.Contains("## Configuration Values by Source File", markdown);
        Assert.Contains("## Suggested Files to Review First", markdown);
        Assert.Contains("## Migration Considerations", markdown);
        Assert.Contains("## Suggested Questions to Ask the Team", markdown);
        Assert.Contains("## Notes and Limitations", markdown);

        Assert.Contains("| Configuration findings | 0 |", markdown);
        Assert.Contains("| Configuration files | 0 |", markdown);
        Assert.Contains("| Categories with findings | 0 |", markdown);
        Assert.Contains("| Potential migration concerns | 0 |", markdown);
        Assert.Contains("No configuration inventory findings were identified by the current static rules.", markdown);
        Assert.Contains("No configuration findings were produced.", markdown);
    }

    [Fact]
    public void Write_IncludesAnalysisScope()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(Array.Empty<ConfigurationInventoryFinding>());

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Analysis mode | Static / no-build |", markdown);
        Assert.Contains("| Application run | No |", markdown);
        Assert.Contains("| Config transforms applied | No |", markdown);
        Assert.Contains("| External systems validated | No |", markdown);
        Assert.Contains("| Secret values printed | No |", markdown);
        Assert.Contains("| Runtime usage proven | No |", markdown);
        Assert.Contains("| Completeness guarantee | No |", markdown);
    }

    [Fact]
    public void Write_WhenReportHasFindings_WritesSummaryTablesAndGroupedSourceFileFindings()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = CreateReport();

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Configuration findings | 5 |", markdown);
        Assert.Contains("| Configuration files | 1 |", markdown);
        Assert.Contains("| Categories with findings | 5 |", markdown);
        Assert.Contains("| Potential migration concerns | 5 |", markdown);

        Assert.Contains("### SampleLegacyApp.Web — Web.config", markdown);
        Assert.Contains("#### App Setting", markdown);
        Assert.Contains("#### Connection String", markdown);
        Assert.Contains("#### Configuration File", markdown);
        Assert.Contains("#### JSON Configuration", markdown);
        Assert.Contains("#### Configuration API Usage", markdown);

        Assert.Contains("| Name | Value | Evidence | Requires Review |", markdown);
        Assert.Contains("| ApiBaseUrl | https://api.example.test | App setting configured. | Yes |", markdown);
        Assert.Contains("| MainDatabase | Server=.;Password=***; | Connection string configured. | Yes |", markdown);
        Assert.Contains("| Web.config | N/A | Configuration file found. | Yes |", markdown);
        Assert.Contains("| ConnectionStrings:RabbitMQ | amqp://***:***@rabbitmq-dev:5672/sample | JSON setting configured. | Yes |", markdown);
        Assert.Contains("ConfigurationManager.AppSettings", markdown);
    }

    [Fact]
    public void Write_WritesConfigurationOverviewGroupedByCategory()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = CreateReport();

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Category | Findings |", markdown);
        Assert.Contains("| App Setting | 1 |", markdown);
        Assert.Contains("| Configuration API Usage | 1 |", markdown);
        Assert.Contains("| Configuration File | 1 |", markdown);
        Assert.Contains("| Connection String | 1 |", markdown);
        Assert.Contains("| JSON Configuration | 1 |", markdown);
    }

    [Fact]
    public void Write_GroupsFindingsBySourceFileAndCategory()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(
        [
            CreateFinding(ConfigurationInventoryCategory.WcfConfiguration, "system.serviceModel", @"C:\Repo\SampleLegacyApp.Web\Web.config"),
            CreateFinding(ConfigurationInventoryCategory.AspNetIisConfiguration, "system.web", @"C:\Repo\SampleLegacyApp.Web\Web.config"),
            CreateFinding(ConfigurationInventoryCategory.EnvironmentTransform, "Web.Release.config", @"C:\Repo\SampleLegacyApp.Web\Web.Release.config"),
            CreateFinding(ConfigurationInventoryCategory.JsonConfiguration, "RabbitMQ:HostName", @"C:\Repo\SampleLegacyApp.Web\appsettings.json", "rabbitmq-dev")
        ]);

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("### SampleLegacyApp.Web — Web.config", markdown);
        Assert.Contains("#### WCF Configuration", markdown);
        Assert.Contains("#### ASP.NET / IIS Configuration", markdown);

        Assert.Contains("### SampleLegacyApp.Web — Web.Release.config", markdown);
        Assert.Contains("#### Environment Transform", markdown);

        Assert.Contains("### SampleLegacyApp.Web — appsettings.json", markdown);
        Assert.Contains("#### JSON Configuration", markdown);
        Assert.Contains("| RabbitMQ:HostName | rabbitmq-dev | RabbitMQ:HostName evidence found. | Yes |", markdown);
    }

    [Fact]
    public void Write_RendersStructuralFindingsWithNotApplicableValue()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(
        [
            CreateFinding(ConfigurationInventoryCategory.ConfigurationFile, "Web.config", @"C:\Repo\SampleLegacyApp.Web\Web.config")
        ]);

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Web.config | N/A | Web.config evidence found. | Yes |", markdown);
        Assert.DoesNotContain("| Web.config | Unknown |", markdown);
    }

    [Fact]
    public void Write_WritesSuggestedFilesToReviewFirst()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = CreateReport();

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Suggested Files to Review First", markdown);
        Assert.Contains(@"C:\Repo\SampleLegacyApp.Web\Web.config", markdown);
        Assert.Contains("| Project | Source File | Findings | Requires Review | Categories | Source Path |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | Web.config |", markdown);
    }

    [Fact]
    public void Write_WritesMigrationConsiderations()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = CreateReport();

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Migration Considerations", markdown);
        Assert.Contains("Review app settings before migration.", markdown);
        Assert.Contains("Review connection strings before migration.", markdown);
    }

    [Fact]
    public void Write_WritesSuggestedQuestions()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(Array.Empty<ConfigurationInventoryFinding>());

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Which configuration files are used in each environment?", markdown);
        Assert.Contains("Which settings are secrets and where should they be stored after migration?", markdown);
        Assert.Contains("Are custom configuration sections still actively used?", markdown);
    }

    [Fact]
    public void Write_WritesNotesAndLimitations()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(Array.Empty<ConfigurationInventoryFinding>());

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("LegacyLens.NET did not run the application.", markdown);
        Assert.Contains("LegacyLens.NET did not apply configuration transforms.", markdown);
        Assert.Contains("LegacyLens.NET did not validate credentials", markdown);
        Assert.Contains("LegacyLens.NET did not prove that a setting is used or unused at runtime.", markdown);
        Assert.Contains("Values are shown only when a scalar value is visible to static analysis", markdown);
        Assert.Contains("Sensitive values should remain masked or redacted", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownPipes()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(
        [
            new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.AppSetting,
                "Api|Base|Url",
                ConfigurationInventorySourceType.Configuration,
                @"C:\Repo\SampleLegacyApp.Web\Web.config",
                "Sample|Web",
                "Evidence | contains pipe",
                "Value|With|Pipe",
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review this value.")
        ]);

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Api\\|Base\\|Url", markdown);
        Assert.Contains("Sample\\|Web", markdown);
        Assert.Contains("Evidence \\| contains pipe", markdown);
        Assert.Contains("Value\\|With\\|Pipe", markdown);
    }

    [Fact]
    public void Write_DoesNotWriteRawSecretsWhenReportContainsMaskedValues()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(
        [
            new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.ConnectionString,
                "MainDatabase",
                ConfigurationInventorySourceType.Configuration,
                @"C:\Repo\SampleLegacyApp.Web\Web.config",
                "SampleLegacyApp.Web",
                "Connection string configured.",
                "Server=.;Password=***;",
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review connection strings before migration.")
        ]);

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Password=***", markdown);
        Assert.DoesNotContain("SuperSecret123", markdown);
        Assert.DoesNotContain("plain-db-password", markdown);
    }


    [Fact]
    public void Write_WhenReportHasSourceUsages_WritesSourceUsageAndReconciliationSections()
    {
        var outputPath = Path.Combine(_tempDirectory, "configuration-inventory.md");

        var report = new ConfigurationInventoryReport(
            [
                new ConfigurationInventoryFinding(
                    ConfigurationInventoryCategory.AppSetting,
                    "ApiBaseUrl",
                    ConfigurationInventorySourceType.Configuration,
                    @"C:\Repo\SampleLegacyApp.Web\Web.config",
                    "SampleLegacyApp.Web",
                    "App setting configured.",
                    "https://api.example.test",
                    ConfigurationInventoryConfidence.High,
                    RequiresReview: true,
                    MigrationConsideration: "Review app settings before migration.")
            ],
            [
                new ConfigurationUsageFinding(
                    ConfigurationUsageKind.AppSetting,
                    "ApiBaseUrl",
                    ConfigurationUsageKeyResolution.MatchedVisibleConfigurationEntry,
                    "SampleLegacyApp.Web",
                    @"C:\Repo\SampleLegacyApp.Web\SettingsReader.cs",
                    18,
                    @"ConfigurationManager.AppSettings[""ApiBaseUrl""]",
                    RequiresReview: false),

                new ConfigurationUsageFinding(
                    ConfigurationUsageKind.AppSetting,
                    null,
                    ConfigurationUsageKeyResolution.DynamicKeyRequiresReview,
                    "SampleLegacyApp.Web",
                    @"C:\Repo\SampleLegacyApp.Web\SettingsReader.cs",
                    25,
                    "ConfigurationManager.AppSettings[key]",
                    RequiresReview: true)
            ],
            [
                new ConfigurationKeyReconciliation(
                    ConfigurationUsageKind.AppSetting,
                    "ApiBaseUrl",
                    @"C:\Repo\SampleLegacyApp.Web\Web.config",
                    ConfigurationStaticSourceUsage.Found,
                    "Literal source usage matched."),

                new ConfigurationKeyReconciliation(
                    ConfigurationUsageKind.AppSetting,
                    "FeatureXEnabled",
                    @"C:\Repo\SampleLegacyApp.Web\Web.config",
                    ConfigurationStaticSourceUsage.NoStaticSourceUsageDetected,
                    "This does not prove the key is unused.")
            ]);

        var writer = new ConfigurationInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Source Code Configuration Usage", markdown);
        Assert.Contains("## Configuration Key Reconciliation", markdown);
        Assert.Contains("| App setting usages | 2 |", markdown);
        Assert.Contains("| Matched visible keys | 1 |", markdown);
        Assert.Contains("| Dynamic usages requiring review | 1 |", markdown);
        Assert.Contains(@"| App setting | ApiBaseUrl | Matched visible configuration entry | SampleLegacyApp.Web | `SettingsReader.cs` | 18 | `ConfigurationManager.AppSettings[""ApiBaseUrl""]` | No |", markdown);
        Assert.Contains("| App setting | Dynamic / unknown | Dynamic key requires review | SampleLegacyApp.Web | `SettingsReader.cs` | 25 | `ConfigurationManager.AppSettings[key]` | Yes |", markdown);
        Assert.Contains("| App setting | FeatureXEnabled | `Web.config` | No static source usage detected | This does not prove the key is unused. |", markdown);
    }

    private static ConfigurationInventoryReport CreateReport()
    {
        return new ConfigurationInventoryReport(
        [
            new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.ConfigurationFile,
                "Web.config",
                ConfigurationInventorySourceType.Configuration,
                @"C:\Repo\SampleLegacyApp.Web\Web.config",
                "SampleLegacyApp.Web",
                "Configuration file found.",
                null,
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review configuration files before migration."),

            new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.AppSetting,
                "ApiBaseUrl",
                ConfigurationInventorySourceType.Configuration,
                @"C:\Repo\SampleLegacyApp.Web\Web.config",
                "SampleLegacyApp.Web",
                "App setting configured.",
                "https://api.example.test",
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review app settings before migration."),

            new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.ConnectionString,
                "MainDatabase",
                ConfigurationInventorySourceType.Configuration,
                @"C:\Repo\SampleLegacyApp.Web\Web.config",
                "SampleLegacyApp.Web",
                "Connection string configured.",
                "Server=.;Password=***;",
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review connection strings before migration."),

            new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.JsonConfiguration,
                "ConnectionStrings:RabbitMQ",
                ConfigurationInventorySourceType.JsonConfiguration,
                @"C:\Repo\SampleLegacyApp.Web\appsettings.json",
                "SampleLegacyApp.Web",
                "JSON setting configured.",
                "amqp://***:***@rabbitmq-dev:5672/sample",
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review JSON configuration before migration."),

            new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.ConfigurationApiUsage,
                "ConfigurationManager.AppSettings",
                ConfigurationInventorySourceType.SourceCode,
                @"C:\Repo\SampleLegacyApp.Web\SettingsReader.cs",
                "SampleLegacyApp.Web",
                "ConfigurationManager.AppSettings usage found.",
                null,
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review configuration API usage before migration.")
        ]);
    }

    private static ConfigurationInventoryFinding CreateFinding(
        ConfigurationInventoryCategory category,
        string name,
        string sourcePath,
        string? value = null)
    {
        return new ConfigurationInventoryFinding(
            category,
            name,
            ConfigurationInventorySourceType.Configuration,
            sourcePath,
            "SampleLegacyApp.Web",
            $"{name} evidence found.",
            value,
            ConfigurationInventoryConfidence.High,
            RequiresReview: true,
            MigrationConsideration: $"{name} requires review.");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}



