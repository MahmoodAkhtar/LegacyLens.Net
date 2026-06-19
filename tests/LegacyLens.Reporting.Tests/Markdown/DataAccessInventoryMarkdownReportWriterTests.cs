using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class DataAccessInventoryMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public DataAccessInventoryMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.DataAccessInventoryMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_WhenOutputPathIsEmpty_ThrowsArgumentException()
    {
        var writer = new DataAccessInventoryMarkdownReportWriter();

        var report = new DataAccessInventoryReport(Array.Empty<DataAccessFinding>());

        Assert.Throws<ArgumentException>(() => writer.Write("", report));
    }

    [Fact]
    public void Write_WhenReportIsNull_ThrowsArgumentNullException()
    {
        var writer = new DataAccessInventoryMarkdownReportWriter();

        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var exception = Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));

        Assert.Equal("report", exception.ParamName);
    }

    [Fact]
    public void Write_WhenDirectoryDoesNotExist_CreatesDirectoryAndWritesReport()
    {
        var outputDirectory = Path.Combine(_tempDirectory, "reports", "data-access");
        var outputPath = Path.Combine(outputDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(Array.Empty<DataAccessFinding>());

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        Assert.True(Directory.Exists(outputDirectory));
        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Write_WhenReportIsEmpty_WritesExpectedHeadingsAndNoFindingsText()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(Array.Empty<DataAccessFinding>());

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Data Access Inventory", markdown);
        Assert.Contains("## Summary", markdown);
        Assert.Contains("## Analysis Scope", markdown);
        Assert.Contains("## Data Access Overview", markdown);
        Assert.Contains("## Projects with Data Access Indicators", markdown);
        Assert.Contains("## Data Access Findings", markdown);
        Assert.Contains("## Connection Strings", markdown);
        Assert.Contains("## Database Provider Indicators", markdown);
        Assert.Contains("## ORM and Data Access Technologies", markdown);
        Assert.Contains("## EF / EDMX Details", markdown);
        Assert.Contains("## ADO.NET Indicators", markdown);
        Assert.Contains("## Raw SQL and Stored Procedure Indicators", markdown);
        Assert.Contains("## Repository and Unit-of-Work Candidates", markdown);
        Assert.Contains("## Migration Artifacts", markdown);
        Assert.Contains("## Suggested Files to Review First", markdown);
        Assert.Contains("## Migration Considerations", markdown);
        Assert.Contains("## Suggested Questions to Ask the Team", markdown);
        Assert.Contains("## Notes and Limitations", markdown);

        Assert.Contains("No data access findings were produced by the current static rules.", markdown);
        Assert.Contains("No project-specific data access findings were produced.", markdown);
        Assert.Contains("No visible data access technologies, patterns, or migration concerns were identified by the current static rules.", markdown);
        Assert.Contains("No findings in this category.", markdown);
        Assert.Contains("No files were suggested because no data access findings were produced.", markdown);
        Assert.Contains("- No migration considerations were produced by the current static rules.", markdown);
    }

    [Fact]
    public void Write_WhenReportHasFindings_WritesSummaryTablesAndFindings()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.ConnectionString,
                    "MainDatabase",
                    DataAccessSourceType.Configuration,
                    @"C:\Repo\SampleLegacyApp.Web\Web.config",
                    "Connection string configured with provider System.Data.SqlClient.",
                    maskedValue: "Server=.;Database=Main;User Id=***;Password=***;",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "Connection string should be verified by the development team before migration or environment setup."),

                CreateFinding(
                    DataAccessCategory.DatabaseProvider,
                    "System.Data.SqlClient",
                    DataAccessSourceType.Configuration,
                    @"C:\Repo\SampleLegacyApp.Web\Web.config",
                    "Connection string providerName is System.Data.SqlClient.",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "SQL Server provider detected. Review provider package, connection-string format, authentication, and EF/Dapper/ADO.NET usage before migration."),

                CreateFinding(
                    DataAccessCategory.EntityFramework6,
                    "EntityFramework",
                    DataAccessSourceType.PackageReference,
                    @"C:\Repo\SampleLegacyApp.Data\packages.config",
                    "EntityFramework 6.4.4 package reference found from packages.config.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "Classic Entity Framework detected. Review EF6 usage, EDMX/ObjectContext/DbContext patterns, and whether EF6 can remain isolated or needs migration."),

                CreateFinding(
                    DataAccessCategory.Dapper,
                    "Dapper",
                    DataAccessSourceType.PackageReference,
                    @"C:\Repo\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
                    "Dapper 2.1.35 package reference found from PackageReference.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "Dapper package detected. Review raw SQL, stored procedure usage, connection management, and transaction boundaries."),

                CreateFinding(
                    DataAccessCategory.RepositoryPattern,
                    "CustomerRepository",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\SampleLegacyApp.Data\CustomerRepository.cs",
                    "Repository class or interface candidate found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Low,
                    migrationConsideration: "Repository candidates should be reviewed to understand persistence boundaries and whether queries are centralised or spread through the application.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Data access findings | 5 |", markdown);
        Assert.Contains("| Categories with findings | 5 |", markdown);
        Assert.Contains("| Projects with findings | 1 |", markdown);

        Assert.Contains("| Connection String | 1 |", markdown);
        Assert.Contains("| Database Provider | 1 |", markdown);
        Assert.Contains("| Entity Framework 6 | 1 |", markdown);
        Assert.Contains("| Dapper | 1 |", markdown);
        Assert.Contains("| Repository Pattern | 1 |", markdown);

        Assert.Contains("| SampleLegacyApp.Data | 3 | Dapper, Entity Framework 6, Repository Pattern |", markdown);

        Assert.Contains("| Connection String | MainDatabase | Configuration: `C:\\Repo\\SampleLegacyApp.Web\\Web.config` |", markdown);
        Assert.Contains("| Entity Framework 6 | EntityFramework | PackageReference: `C:\\Repo\\SampleLegacyApp.Data\\packages.config` | SampleLegacyApp.Data | `EntityFramework 6.4.4 package reference found from packages.config.`", markdown);
        Assert.Contains("| Dapper | Dapper | PackageReference: `C:\\Repo\\SampleLegacyApp.Data\\SampleLegacyApp.Data.csproj` | SampleLegacyApp.Data | `Dapper 2.1.35 package reference found from PackageReference.`", markdown);
        Assert.Contains("| Repository Pattern | CustomerRepository | SourceCode: `C:\\Repo\\SampleLegacyApp.Data\\CustomerRepository.cs` | SampleLegacyApp.Data | `Repository class or interface candidate found.`", markdown);

        Assert.Contains("Server=.;Database=Main;User Id=***;Password=***;", markdown);
        Assert.Contains("Classic Entity Framework detected.", markdown);
        Assert.Contains("Dapper package detected.", markdown);
        Assert.Contains("Repository candidates should be reviewed", markdown);
    }

    [Fact]
    public void Write_IncludesAnalysisScope()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(Array.Empty<DataAccessFinding>());

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Analysis mode | Static / no-build |", markdown);
        Assert.Contains("| Database connection attempted | No |", markdown);
        Assert.Contains("| SQL executed | No |", markdown);
        Assert.Contains("| Schema inspected | No |", markdown);
        Assert.Contains("| Runtime usage proven | No |", markdown);
        Assert.Contains("| Compatibility guarantee | No |", markdown);
    }

    [Fact]
    public void Write_IncludesConnectionStringSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.ConnectionString,
                    "MainDatabase",
                    DataAccessSourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured.",
                    maskedValue: "Server=.;Database=Main;Password=***;",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "Connection string should be verified by the development team before migration or environment setup.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Connection Strings", markdown);
        Assert.Contains("| Connection String | MainDatabase | Configuration: `C:\\Repo\\Web.config` |", markdown);
        Assert.Contains("Connection string configured.", markdown);
    }

    [Fact]
    public void Write_IncludesDatabaseProviderSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.DatabaseProvider,
                    "Microsoft.Data.SqlClient",
                    DataAccessSourceType.PackageReference,
                    @"C:\Repo\Data.csproj",
                    "Microsoft.Data.SqlClient 5.2.2 package reference found from PackageReference.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Medium,
                    migrationConsideration: "Database provider package detected. Review provider compatibility, connection-string format, authentication, and deployment requirements.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Database Provider Indicators", markdown);
        Assert.Contains("| Database Provider | Microsoft.Data.SqlClient | PackageReference: `C:\\Repo\\Data.csproj` | SampleLegacyApp.Data | `Microsoft.Data.SqlClient 5.2.2 package reference found from PackageReference.`", markdown);
    }

    [Fact]
    public void Write_IncludesOrmAndDataAccessTechnologiesSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.EntityFrameworkCore,
                    "Microsoft.EntityFrameworkCore",
                    DataAccessSourceType.PackageReference,
                    @"C:\Repo\Data.csproj",
                    "Microsoft.EntityFrameworkCore 8.0.6 package reference found from PackageReference.",
                    projectName: "ModernApp.Data",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "EF Core package detected. Review DbContext, migrations, provider packages, and target framework alignment."),

                CreateFinding(
                    DataAccessCategory.NHibernate,
                    "NHibernate",
                    DataAccessSourceType.PackageReference,
                    @"C:\Repo\LegacyData.csproj",
                    "NHibernate 5.5.2 package reference found from PackageReference.",
                    projectName: "LegacyApp.Data",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "NHibernate package detected. Review mappings, session factory configuration, transaction handling, and provider compatibility.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## ORM and Data Access Technologies", markdown);
        Assert.Contains("| Entity Framework Core | Microsoft.EntityFrameworkCore | PackageReference: `C:\\Repo\\Data.csproj` | ModernApp.Data |", markdown);
        Assert.Contains("| NHibernate | NHibernate | PackageReference: `C:\\Repo\\LegacyData.csproj` | LegacyApp.Data |", markdown);
    }

    [Fact]
    public void Write_IncludesEfEdmxDetailsSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.EdmxObjectContext,
                    "LegacyModel.edmx",
                    DataAccessSourceType.EdmxFile,
                    @"C:\Repo\Data\LegacyModel.edmx",
                    "EDMX model file found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "EDMX models are classic EF artifacts. Review ObjectContext/entities/mappings before planning EF Core migration.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## EF / EDMX Details", markdown);
        Assert.Contains("| EDMX / ObjectContext | LegacyModel.edmx | EDMX: `C:\\Repo\\Data\\LegacyModel.edmx` | SampleLegacyApp.Data | `EDMX model file found.`", markdown);
    }

    [Fact]
    public void Write_IncludesAdoNetSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.AdoNet,
                    "System.Data",
                    DataAccessSourceType.AssemblyReference,
                    @"C:\Repo\Data.csproj",
                    "System.Data assembly reference found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Medium,
                    migrationConsideration: "ADO.NET-related assembly reference detected. Review direct connection, command, transaction, and data reader usage.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## ADO.NET Indicators", markdown);
        Assert.Contains("| ADO.NET | System.Data | AssemblyReference: `C:\\Repo\\Data.csproj` | SampleLegacyApp.Data | `System.Data assembly reference found.`", markdown);
    }

    [Fact]
    public void Write_IncludesRawSqlAndStoredProcedureSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.RawSql,
                    "RawSqlQuery.cs",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\Data\RawSqlQuery.cs",
                    "Possible raw SQL string detected.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Low,
                    migrationConsideration: "Raw SQL indicators should be reviewed for SQL dialect, parameter handling, stored procedure calls, and provider compatibility."),

                CreateFinding(
                    DataAccessCategory.StoredProcedure,
                    "StoredProcedureRunner.cs",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\Data\StoredProcedureRunner.cs",
                    "Possible stored procedure usage detected.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Low,
                    migrationConsideration: "Stored procedure indicators should be verified by the development team before changing data access code or schema deployment.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Raw SQL and Stored Procedure Indicators", markdown);
        Assert.Contains("| Raw SQL | RawSqlQuery.cs | SourceCode: `C:\\Repo\\Data\\RawSqlQuery.cs` | SampleLegacyApp.Data | `Possible raw SQL string detected.`", markdown);
        Assert.Contains("| Stored Procedure | StoredProcedureRunner.cs | SourceCode: `C:\\Repo\\Data\\StoredProcedureRunner.cs` | SampleLegacyApp.Data | `Possible stored procedure usage detected.`", markdown);
    }

    [Fact]
    public void Write_IncludesRepositoryAndUnitOfWorkSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.RepositoryPattern,
                    "CustomerRepository",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\Data\CustomerRepository.cs",
                    "Repository class or interface candidate found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Low,
                    migrationConsideration: "Repository candidates should be reviewed to understand persistence boundaries and whether queries are centralised or spread through the application."),

                CreateFinding(
                    DataAccessCategory.UnitOfWorkPattern,
                    "CustomerUnitOfWork",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\Data\CustomerUnitOfWork.cs",
                    "Unit-of-work class or interface candidate found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Low,
                    migrationConsideration: "Unit-of-work candidates should be reviewed to understand transaction boundaries and persistence orchestration.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Repository and Unit-of-Work Candidates", markdown);
        Assert.Contains("| Repository Pattern | CustomerRepository | SourceCode: `C:\\Repo\\Data\\CustomerRepository.cs` | SampleLegacyApp.Data | `Repository class or interface candidate found.`", markdown);
        Assert.Contains("| Unit of Work Pattern | CustomerUnitOfWork | SourceCode: `C:\\Repo\\Data\\CustomerUnitOfWork.cs` | SampleLegacyApp.Data | `Unit-of-work class or interface candidate found.`", markdown);
    }

    [Fact]
    public void Write_IncludesMigrationArtifactsSection()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.MigrationArtifact,
                    "Migrations",
                    DataAccessSourceType.MigrationFolder,
                    @"C:\Repo\Data\Migrations",
                    "Migrations folder found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Medium,
                    migrationConsideration: "Migration artifacts should be reviewed to understand schema evolution history and EF migration strategy.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Migration Artifacts", markdown);
        Assert.Contains("| Migration Artifact | Migrations | MigrationFolder: `C:\\Repo\\Data\\Migrations` | SampleLegacyApp.Data | `Migrations folder found.`", markdown);
    }

    [Fact]
    public void Write_IncludesSuggestedFilesToReviewFirst()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.RepositoryPattern,
                    "CustomerRepository",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\Data\CustomerRepository.cs",
                    "Repository class or interface candidate found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Low,
                    migrationConsideration: "Repository candidates should be reviewed."),

                CreateFinding(
                    DataAccessCategory.ConnectionString,
                    "MainDatabase",
                    DataAccessSourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured.",
                    maskedValue: "Server=.;Database=Main;Password=***;",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "Connection string should be verified.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Suggested Files to Review First", markdown);
        Assert.Contains("| Priority | File / Source | Reason |", markdown);
        Assert.Contains("| 1 | `C:\\Repo\\Web.config` | Connection String: Connection string configured. |", markdown);
        Assert.Contains("| 2 | `C:\\Repo\\Data\\CustomerRepository.cs` | Repository Pattern: Repository class or interface candidate found. |", markdown);
    }

    [Fact]
    public void Write_IncludesMigrationConsiderations()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.Dapper,
                    "Dapper",
                    DataAccessSourceType.PackageReference,
                    @"C:\Repo\Data.csproj",
                    "Dapper package reference found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "Dapper package detected. Review raw SQL, stored procedure usage, connection management, and transaction boundaries.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Migration Considerations", markdown);
        Assert.Contains("- Dapper package detected. Review raw SQL, stored procedure usage, connection management, and transaction boundaries.", markdown);
    }

    [Fact]
    public void Write_DeduplicatesMigrationConsiderations()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        const string migrationConsideration =
            "Dapper package detected. Review raw SQL, stored procedure usage, connection management, and transaction boundaries.";

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.Dapper,
                    "Dapper",
                    DataAccessSourceType.PackageReference,
                    @"C:\Repo\Data.csproj",
                    "Dapper package reference found.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: migrationConsideration),

                CreateFinding(
                    DataAccessCategory.Dapper,
                    "Dapper usage detected.",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\Data\CustomerQuery.cs",
                    "Dapper token or common Dapper call found in source.",
                    projectName: "SampleLegacyApp.Data",
                    confidence: DataAccessConfidence.Medium,
                    migrationConsideration: migrationConsideration)
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Equal(
            1,
            CountOccurrences(
                markdown,
                "- Dapper package detected. Review raw SQL, stored procedure usage, connection management, and transaction boundaries."));
    }

    [Fact]
    public void Write_IncludesSuggestedQuestions()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(Array.Empty<DataAccessFinding>());

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Suggested Questions to Ask the Team", markdown);
        Assert.Contains("- Which databases are required for local development, test, staging, and production?", markdown);
        Assert.Contains("- Are connection strings environment-specific, transformed at deployment, or supplied by secret stores?", markdown);
        Assert.Contains("- Which ORM or data access technology is considered the source of truth for new development?", markdown);
        Assert.Contains("- Are stored procedures part of the application contract or owned by a separate database team?", markdown);
        Assert.Contains("- Are EF migrations used, or is schema deployment handled separately?", markdown);
        Assert.Contains("- Are repositories and unit-of-work classes still active, or are they legacy abstractions?", markdown);
    }

    [Fact]
    public void Write_IncludesNotesAndLimitations()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(Array.Empty<DataAccessFinding>());

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Notes and Limitations", markdown);
        Assert.Contains("- This report is based on static discovery only.", markdown);
        Assert.Contains("- LegacyLens.NET did not connect to databases.", markdown);
        Assert.Contains("- LegacyLens.NET did not validate credentials or connection strings.", markdown);
        Assert.Contains("- LegacyLens.NET did not execute SQL.", markdown);
        Assert.Contains("- LegacyLens.NET did not parse or validate full SQL syntax.", markdown);
        Assert.Contains("- LegacyLens.NET did not inspect live database schemas.", markdown);
        Assert.Contains("- LegacyLens.NET did not run EF migrations or scaffold EF Core models.", markdown);
        Assert.Contains("- Findings should be verified by the development team before migration or refactoring decisions are made.", markdown);
        Assert.Contains("- Sensitive values should be masked or redacted where discovered by supported scanners.", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownTablePipes()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.RawSql,
                    "Query|WithPipe",
                    DataAccessSourceType.SourceCode,
                    @"C:\Repo\Data\Raw|Sql.cs",
                    "Evidence contains A|B.",
                    projectName: "Legacy|Data",
                    maskedValue: "Server=.;Database=A|B;Password=***;",
                    confidence: DataAccessConfidence.Low,
                    migrationConsideration: "Migration consideration contains X|Y.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Query\\|WithPipe", markdown);
        Assert.Contains("Legacy\\|Data", markdown);
        Assert.Contains("Evidence contains A\\|B.", markdown);
        Assert.Contains("Server=.;Database=A\\|B;Password=***;", markdown);
        Assert.Contains("Migration consideration contains X\\|Y.", markdown);
        Assert.Contains("`C:\\Repo\\Data\\Raw\\|Sql.cs`", markdown);
    }

    [Fact]
    public void Write_DoesNotWriteRawSecretsWhenReportContainsMaskedValues()
    {
        var outputPath = Path.Combine(_tempDirectory, "data-access-inventory.md");

        var report = new DataAccessInventoryReport(
            new[]
            {
                CreateFinding(
                    DataAccessCategory.ConnectionString,
                    "MainDatabase",
                    DataAccessSourceType.Configuration,
                    @"C:\Repo\Web.config",
                    "Connection string configured.",
                    maskedValue: "Server=.;Database=Main;User Id=***;Password=***;",
                    confidence: DataAccessConfidence.High,
                    migrationConsideration: "Connection string should be verified by the development team before migration or environment setup.")
            });

        var writer = new DataAccessInventoryMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Password=***", markdown);
        Assert.Contains("User Id=***", markdown);
        Assert.DoesNotContain("real-password", markdown);
        Assert.DoesNotContain("admin-password", markdown);
        Assert.DoesNotContain("super-secret", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static DataAccessFinding CreateFinding(
        DataAccessCategory category,
        string name,
        DataAccessSourceType sourceType,
        string sourcePath,
        string evidence,
        string? projectName = null,
        string? maskedValue = null,
        DataAccessConfidence confidence = DataAccessConfidence.Medium,
        string migrationConsideration = "Requires review.")
    {
        return new DataAccessFinding(
            category,
            name,
            sourceType,
            sourcePath,
            projectName,
            evidence,
            maskedValue,
            confidence,
            migrationConsideration);
    }

    private static int CountOccurrences(string value, string search)
    {
        var count = 0;
        var index = 0;

        while (index < value.Length)
        {
            var foundIndex = value.IndexOf(search, index, StringComparison.Ordinal);

            if (foundIndex < 0)
            {
                return count;
            }

            count++;
            index = foundIndex + search.Length;
        }

        return count;
    }
}
