using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class ExternalDependenciesMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public ExternalDependenciesMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ExternalDependenciesMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new ExternalDependenciesMarkdownReportWriter();

        var report = new ExternalDependenciesReport(Array.Empty<ExternalDependency>());

        Assert.Throws<ArgumentException>(() => writer.Write("", report));
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new ExternalDependenciesMarkdownReportWriter();

        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WhenDirectoryDoesNotExist_CreatesDirectoryAndWritesReport()
    {
        var outputDirectory = Path.Combine(_tempDirectory, "reports", "external");
        var outputPath = Path.Combine(outputDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(Array.Empty<ExternalDependency>());

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        Assert.True(Directory.Exists(outputDirectory));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Write_WhenReportIsEmpty_WritesExpectedHeadingsAndNoFindingsText()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(Array.Empty<ExternalDependency>());

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# External Dependencies", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Analysis Scope", markdown);
        Assert.Contains("## Dependency Overview", markdown);
        Assert.Contains("## Dependencies", markdown);
        Assert.Contains("## Database Dependencies", markdown);
        Assert.Contains("## HTTP / Service Dependencies", markdown);
        Assert.Contains("## WCF Dependencies", markdown);
        Assert.Contains("## Messaging Dependencies", markdown);
        Assert.Contains("## File System Dependencies", markdown);
        Assert.Contains("## Email Dependencies", markdown);
        Assert.Contains("## Cache / Distributed State Dependencies", markdown);
        Assert.Contains("## Authentication / Identity Provider Dependencies", markdown);
        Assert.Contains("## Cloud Service Dependencies", markdown);
        Assert.Contains("## Build-Time / Package Feed Dependencies", markdown);
        Assert.Contains("## External Assembly / Vendor DLL Dependencies", markdown);
        Assert.Contains("## Unknown / Requires Review Dependencies", markdown);
        Assert.Contains("## Suggested Questions to Ask the Team", markdown);
        Assert.Contains("## Notes and Limitations", markdown);

        Assert.Contains("| Possible external dependencies | 0 |", markdown);
        Assert.Contains("| Categories with findings | 0 |", markdown);
        Assert.Contains("| Findings requiring confirmation | 0 |", markdown);
        Assert.Contains("No possible external dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No dependency findings were produced.", markdown);
    }

    [Fact]
    public void Write_WritesAnalysisScopeWithStaticNoVerificationLimitations()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(Array.Empty<ExternalDependency>());

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Analysis mode | Static / no-build |", markdown);
        Assert.Contains("| Runtime verification | No |", markdown);
        Assert.Contains("| External systems contacted | No |", markdown);
        Assert.Contains("| Credential validation | No |", markdown);
        Assert.Contains("| Secret values printed | No |", markdown);
        Assert.Contains("| Completeness guarantee | No |", markdown);
    }

    [Fact]
    public void Write_WhenDependenciesExist_WritesSummaryCounts()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.Database,
                    "MainDatabase",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured.",
                    requiresConfirmation: true),
                CreateDependency(
                    ExternalDependencyCategory.HttpApi,
                    "PaymentApiBaseUrl",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "HTTP/API endpoint setting found.",
                    requiresConfirmation: true),
                CreateDependency(
                    ExternalDependencyCategory.HttpApi,
                    "CustomerApiBaseUrl",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "HTTP/API endpoint setting found.",
                    requiresConfirmation: false)
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Possible external dependencies | 3 |", markdown);
        Assert.Contains("| Categories with findings | 2 |", markdown);
        Assert.Contains("| Findings requiring confirmation | 2 |", markdown);
    }

    [Fact]
    public void Write_WhenDependenciesExist_WritesDependencyOverviewGroupedByCategory()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.Database,
                    "MainDatabase",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured."),
                CreateDependency(
                    ExternalDependencyCategory.HttpApi,
                    "PaymentApiBaseUrl",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "HTTP/API endpoint setting found."),
                CreateDependency(
                    ExternalDependencyCategory.HttpApi,
                    "CustomerApiBaseUrl",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "HTTP/API endpoint setting found."),
                CreateDependency(
                    ExternalDependencyCategory.WcfServiceEndpoint,
                    "Legacy.CustomerService",
                    ExternalDependencySourceType.WcfEndpoint,
                    @"C:\Repo\Web.config",
                    "basicHttpBinding endpoint configured.")
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Category | Count | Examples |", markdown);
        Assert.Contains("| Database | 1 | MainDatabase |", markdown);
        Assert.Contains("| HTTP / API | 2 | PaymentApiBaseUrl, CustomerApiBaseUrl |", markdown);
        Assert.Contains("| WCF / Service Endpoint | 1 | Legacy.CustomerService |", markdown);
    }

    [Fact]
    public void Write_WritesMainDependenciesTable()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.Database,
                    "MainDatabase",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured with provider System.Data.SqlClient.",
                    maskedValue: "Server=.;Database=Main;User Id=***;Password=***;",
                    requiresConfirmation: true)
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Category | Name / Identifier | Source | Evidence | Masked Value | Requires Confirmation |",
            markdown);
        Assert.Contains(
            "| Database | MainDatabase | Configuration | `Connection string configured with provider System.Data.SqlClient.` | `Server=.;Database=Main;User Id=***;Password=***;` | Yes |",
            markdown);
    }

    [Fact]
    public void Write_WritesCategorySpecificDependencySection()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.Database,
                    "MainDatabase",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured.",
                    projectName: "Legacy.Web",
                    maskedValue: "Server=.;Database=Main;",
                    confidence: ExternalDependencyConfidence.High,
                    notes: "Runtime usage is not verified.")
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Database Dependencies", markdown);
        Assert.Contains(
            "| Name / Identifier | Source Type | Project | Source File | Evidence | Masked Value | Confidence | Notes |",
            markdown);
        Assert.Contains(
            "| MainDatabase | Configuration | Legacy.Web | `C:\\Repo\\Web.config` | `Connection string configured.` | `Server=.;Database=Main;` | High | Runtime usage is not verified. |",
            markdown);
    }

    [Fact]
    public void Write_WhenEvidenceContainsXmlPipeNewlineAndBackticks_RendersEvidenceWithSharedMarkdownSafeFormatting()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");
        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.WcfServiceEndpoint,
                    "Legacy.CustomerService",
                    ExternalDependencySourceType.WcfEndpoint,
                    @"C:\Repo\Web.config",
                    "<endpoint address=\"http://example.test|api\">\n`binding`\n</endpoint>")
            });
        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("`` <endpoint address=\"http://example.test\\|api\"> `binding` </endpoint> ``", markdown);
    }

    [Fact]
    public void Write_WritesNoFindingsTextForEmptyCategorySections()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.Database,
                    "MainDatabase",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured.")
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("No http / service dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No wcf dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No messaging dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No file system dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No email dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No cache / distributed state dependencies were identified by the current static rules.",
            markdown);
        Assert.Contains(
            "No authentication / identity provider dependencies were identified by the current static rules.",
            markdown);
        Assert.Contains("No cloud service dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No private package feed dependencies were identified by the current static rules.", markdown);
        Assert.Contains("No external assembly / vendor dll dependencies were identified by the current static rules.",
            markdown);
        Assert.Contains("No unknown / requires review dependencies were identified by the current static rules.",
            markdown);
    }

    [Fact]
    public void Write_WritesBuildTimePackageFeedDependenciesSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.PrivatePackageFeed,
                    "PrivateFeed",
                    ExternalDependencySourceType.NuGetConfig,
                    @"C:\Repo\NuGet.config",
                    "Non-nuget.org package source found.",
                    notes: "Private feed availability and credentials require confirmation.")
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Build-Time / Package Feed Dependencies", markdown);
        Assert.Contains("| Source | Evidence | Notes |", markdown);
        Assert.Contains(
            "| `C:\\Repo\\NuGet.config` | `Non-nuget.org package source found.` | Private feed availability and credentials require confirmation. |",
            markdown);
    }

    [Fact]
    public void Write_WritesAllSupportedSourceTypeDisplayNames()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(ExternalDependencyCategory.Database, "Configuration",
                    ExternalDependencySourceType.Configuration, @"C:\Repo\Web.config", "Evidence."),
                CreateDependency(ExternalDependencyCategory.Database, "Package",
                    ExternalDependencySourceType.PackageReference, @"C:\Repo\App.csproj", "Evidence."),
                CreateDependency(ExternalDependencyCategory.Database, "Assembly",
                    ExternalDependencySourceType.AssemblyReference, @"C:\Repo\App.csproj", "Evidence."),
                CreateDependency(ExternalDependencyCategory.Database, "WCF", ExternalDependencySourceType.WcfEndpoint,
                    @"C:\Repo\Web.config", "Evidence."),
                CreateDependency(ExternalDependencyCategory.Database, "NuGet", ExternalDependencySourceType.NuGetConfig,
                    @"C:\Repo\NuGet.config", "Evidence."),
                CreateDependency(ExternalDependencyCategory.Database, "Source", ExternalDependencySourceType.SourceCode,
                    @"C:\Repo\File.cs", "Evidence."),
                CreateDependency(ExternalDependencyCategory.Database, "Project",
                    ExternalDependencySourceType.ProjectFile, @"C:\Repo\App.csproj", "Evidence."),
                CreateDependency(ExternalDependencyCategory.Database, "Unknown", ExternalDependencySourceType.Unknown,
                    string.Empty, "Evidence.")
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Database | Configuration | Configuration | `Evidence.`", markdown);
        Assert.Contains("| Database | Package | Package Reference | `Evidence.`", markdown);
        Assert.Contains("| Database | Assembly | Assembly Reference | `Evidence.`", markdown);
        Assert.Contains("| Database | WCF | WCF Endpoint | `Evidence.`", markdown);
        Assert.Contains("| Database | NuGet | NuGet.config | `Evidence.`", markdown);
        Assert.Contains("| Database | Source | Source Code | `Evidence.`", markdown);
        Assert.Contains("| Database | Project | Project File | `Evidence.`", markdown);
        Assert.Contains("| Database | Unknown | Unknown | `Evidence.`", markdown);
    }

    [Fact]
    public void Write_WritesSuggestedQuestions()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(Array.Empty<ExternalDependency>());

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("- Which of these dependencies are still used in production?", markdown);
        Assert.Contains("- Which databases are shared with other applications?", markdown);
        Assert.Contains("- Are any service URLs environment-specific?", markdown);
        Assert.Contains("- Are WCF endpoints internal only or consumed by third parties?", markdown);
        Assert.Contains("- Are queues, topics, or subscriptions created manually or by infrastructure automation?",
            markdown);
        Assert.Contains("- Are file shares still required?", markdown);
        Assert.Contains("- Where are secrets stored outside this repository?", markdown);
        Assert.Contains("- Which dependencies are required for local development?", markdown);
        Assert.Contains("- Which dependencies are required for CI builds?", markdown);
        Assert.Contains("- Which dependencies are required for production deployment?", markdown);
    }

    [Fact]
    public void Write_WritesNotesAndLimitations()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(Array.Empty<ExternalDependency>());

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("- This report is based on static discovery only.", markdown);
        Assert.Contains("- LegacyLens.NET did not run the application.", markdown);
        Assert.Contains("- LegacyLens.NET did not connect to any external system.", markdown);
        Assert.Contains(
            "- LegacyLens.NET did not validate credentials, URLs, database servers, queues, caches, SMTP servers, file shares, cloud resources, or package feeds.",
            markdown);
        Assert.Contains("- LegacyLens.NET did not inspect production infrastructure.", markdown);
        Assert.Contains("- LegacyLens.NET did not prove that a dependency is active in production.", markdown);
        Assert.Contains("- LegacyLens.NET did not prove that a dependency is unused.", markdown);
        Assert.Contains(
            "- Values that look sensitive should be masked or redacted before being written to this report.", markdown);
        Assert.Contains(
            "- A dependency listed here means evidence was found, not that the dependency is confirmed active in production.",
            markdown);
        Assert.Contains("- This report is not a complete dependency map.", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownTablePipes()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.HttpApi,
                    "Payment|Api",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Value contains A|B.",
                    projectName: "Legacy|Web",
                    maskedValue: "https://example.test/a|b",
                    notes: "Notes contain X|Y.")
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Payment\\|Api", markdown);
        Assert.Contains("Legacy\\|Web", markdown);
        Assert.Contains("Value contains A\\|B.", markdown);
        Assert.Contains("`https://example.test/a\\|b`", markdown);
        Assert.Contains("Notes contain X\\|Y.", markdown);
    }

    [Fact]
    public void Write_DoesNotWriteRawSecretsWhenReportContainsMaskedValues()
    {
        var outputPath = Path.Combine(_tempDirectory, "external-dependencies.md");

        var report = new ExternalDependenciesReport(
            new[]
            {
                CreateDependency(
                    ExternalDependencyCategory.Database,
                    "MainDatabase",
                    ExternalDependencySourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured.",
                    maskedValue: "Server=.;Database=Main;User Id=***;Password=***;")
            });

        var writer = new ExternalDependenciesMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Password=***", markdown);
        Assert.DoesNotContain("real-password", markdown);
        Assert.DoesNotContain("admin-password", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static ExternalDependency CreateDependency(
        ExternalDependencyCategory category,
        string name,
        ExternalDependencySourceType sourceType,
        string sourcePath,
        string evidence,
        string? projectName = null,
        string? maskedValue = null,
        ExternalDependencyConfidence confidence = ExternalDependencyConfidence.Medium,
        bool requiresConfirmation = true,
        string notes = "Requires confirmation.")
    {
        return new ExternalDependency(
            category,
            name,
            sourceType,
            sourcePath,
            projectName,
            evidence,
            maskedValue,
            confidence,
            requiresConfirmation,
            notes);
    }
}
