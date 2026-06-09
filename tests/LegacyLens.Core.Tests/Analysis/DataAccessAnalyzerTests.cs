using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class DataAccessAnalyzerTests
{
    [Fact]
    public void Analyze_WhenProjectsIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new DataAccessAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                null!,
                Array.Empty<DiscoveredConfigFile>()));

        Assert.Equal("projects", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenConfigFilesIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new DataAccessAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                null!));

        Assert.Equal("configFiles", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenNoEvidenceExists_ReturnsEmptyReport()
    {
        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Empty(report.Findings);
    }

    [Fact]
    public void Analyze_WhenConnectionStringExists_AddsConnectionStringFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                ConnectionStringsCount = 1,
                ConnectionStrings =
                {
                    new DiscoveredConnectionString
                    {
                        Name = "MainDatabase",
                        MaskedConnectionString = "Server=.;Database=Main;User Id=***;Password=***;"
                    }
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles);

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.ConnectionString, finding.Category);
        Assert.Equal("MainDatabase", finding.Name);
        Assert.Equal(DataAccessSourceType.Configuration, finding.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", finding.SourcePath);
        Assert.Null(finding.ProjectName);
        Assert.Equal("Connection string configured.", finding.Evidence);
        Assert.Equal("Server=.;Database=Main;User Id=***;Password=***;", finding.MaskedValue);
        Assert.Equal(DataAccessConfidence.High, finding.Confidence);
        Assert.Contains("Connection string", finding.MigrationConsideration, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Analyze_WhenConnectionStringHasProvider_AddsDatabaseProviderFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                ConnectionStringsCount = 1,
                ConnectionStrings =
                {
                    new DiscoveredConnectionString
                    {
                        Name = "MainDatabase",
                        ProviderName = "System.Data.SqlClient",
                        MaskedConnectionString = "Server=.;Database=Main;User Id=***;Password=***;"
                    }
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles);

        Assert.Equal(2, report.Findings.Count);

        var providerFinding = Assert.Single(
            report.Findings,
            x => x.Category == DataAccessCategory.DatabaseProvider);

        Assert.Equal("System.Data.SqlClient", providerFinding.Name);
        Assert.Equal(DataAccessSourceType.Configuration, providerFinding.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", providerFinding.SourcePath);
        Assert.Equal("Connection string providerName is System.Data.SqlClient.", providerFinding.Evidence);
        Assert.Null(providerFinding.MaskedValue);
        Assert.Equal(DataAccessConfidence.High, providerFinding.Confidence);
        Assert.Contains("SQL Server provider detected", providerFinding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenConnectionStringCountExistsWithoutDetails_AddsMediumConfidenceConnectionStringFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\App.config",
                ConnectionStringsCount = 2
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles);

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.ConnectionString, finding.Category);
        Assert.Equal("App.config", finding.Name);
        Assert.Equal(DataAccessSourceType.Configuration, finding.SourceType);
        Assert.Equal(@"C:\Repo\App.config", finding.SourcePath);
        Assert.Equal("2 connection string(s) configured.", finding.Evidence);
        Assert.Null(finding.MaskedValue);
        Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
    }

    [Fact]
    public void Analyze_WhenEntityFrameworkPackageExists_AddsEf6Finding()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "EntityFramework",
                    Version = "6.4.4",
                    SourceFormat = "packages.config",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\packages.config",
                    PackageTargetFramework = "net48"
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.EntityFramework6, finding.Category);
        Assert.Equal("EntityFramework", finding.Name);
        Assert.Equal(DataAccessSourceType.PackageReference, finding.SourceType);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Data\packages.config", finding.SourcePath);
        Assert.Equal("SampleLegacyApp.Data", finding.ProjectName);
        Assert.Equal("EntityFramework 6.4.4 package reference found from packages.config.", finding.Evidence);
        Assert.Equal(DataAccessConfidence.High, finding.Confidence);
        Assert.Contains("Classic Entity Framework", finding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenEfCorePackageExists_AddsEfCoreFinding()
    {
        var project = new DiscoveredProject
        {
            Name = "ModernApp.Data",
            ProjectFilePath = @"C:\Code\ModernApp.Data\ModernApp.Data.csproj",
            TargetFramework = "net8.0",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "Microsoft.EntityFrameworkCore",
                    Version = "8.0.6",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\ModernApp.Data\ModernApp.Data.csproj"
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.EntityFrameworkCore, finding.Category);
        Assert.Equal("Microsoft.EntityFrameworkCore", finding.Name);
        Assert.Equal(DataAccessSourceType.PackageReference, finding.SourceType);
        Assert.Equal(@"C:\Code\ModernApp.Data\ModernApp.Data.csproj", finding.SourcePath);
        Assert.Equal("ModernApp.Data", finding.ProjectName);
        Assert.Equal("Microsoft.EntityFrameworkCore 8.0.6 package reference found from PackageReference.", finding.Evidence);
        Assert.Equal(DataAccessConfidence.High, finding.Confidence);
        Assert.Contains("EF Core package detected", finding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenDapperPackageExists_AddsDapperFinding()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "Dapper",
                    Version = "2.1.35",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj"
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.Dapper, finding.Category);
        Assert.Equal("Dapper", finding.Name);
        Assert.Equal(DataAccessSourceType.PackageReference, finding.SourceType);
        Assert.Equal("SampleLegacyApp.Data", finding.ProjectName);
        Assert.Equal(DataAccessConfidence.High, finding.Confidence);
        Assert.Contains("Dapper package detected", finding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenNHibernatePackageExists_AddsNHibernateFinding()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "NHibernate",
                    Version = "5.5.2",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj"
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.NHibernate, finding.Category);
        Assert.Equal("NHibernate", finding.Name);
        Assert.Equal(DataAccessSourceType.PackageReference, finding.SourceType);
        Assert.Equal("SampleLegacyApp.Data", finding.ProjectName);
        Assert.Equal(DataAccessConfidence.High, finding.Confidence);
        Assert.Contains("NHibernate package detected", finding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenDatabaseProviderPackageExists_AddsDatabaseProviderFinding()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net8.0",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "Microsoft.Data.SqlClient",
                    Version = "5.2.2",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj"
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.DatabaseProvider, finding.Category);
        Assert.Equal("Microsoft.Data.SqlClient", finding.Name);
        Assert.Equal(DataAccessSourceType.PackageReference, finding.SourceType);
        Assert.Equal("SampleLegacyApp.Data", finding.ProjectName);
        Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
        Assert.Contains("Database provider package detected", finding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenLegacyPackageNameExistsWithoutDetails_AddsFinding()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferences =
            {
                "Dapper"
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.Dapper, finding.Category);
        Assert.Equal("Dapper", finding.Name);
        Assert.Equal(DataAccessSourceType.PackageReference, finding.SourceType);
        Assert.Equal(project.ProjectFilePath, finding.SourcePath);
        Assert.Equal("SampleLegacyApp.Data", finding.ProjectName);
        Assert.Equal("Dapper package reference found.", finding.Evidence);
    }

    [Fact]
    public void Analyze_WhenSystemDataAssemblyExists_AddsAdoNetFinding()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.Data"
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.AdoNet, finding.Category);
        Assert.Equal("System.Data", finding.Name);
        Assert.Equal(DataAccessSourceType.AssemblyReference, finding.SourceType);
        Assert.Equal(project.ProjectFilePath, finding.SourcePath);
        Assert.Equal("SampleLegacyApp.Data", finding.ProjectName);
        Assert.Equal("System.Data assembly reference found.", finding.Evidence);
        Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
        Assert.Contains("ADO.NET-related assembly reference detected", finding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenEntityFrameworkAssemblyExists_AddsEf6Finding()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "EntityFramework"
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.EntityFramework6, finding.Category);
        Assert.Equal("EntityFramework", finding.Name);
        Assert.Equal(DataAccessSourceType.AssemblyReference, finding.SourceType);
        Assert.Equal(project.ProjectFilePath, finding.SourcePath);
        Assert.Equal("SampleLegacyApp.Data", finding.ProjectName);
        Assert.Equal("EntityFramework assembly reference found.", finding.Evidence);
        Assert.Equal(DataAccessConfidence.High, finding.Confidence);
        Assert.Contains("Entity Framework assembly reference detected", finding.MigrationConsideration);
    }

    [Fact]
    public void Analyze_WhenEdmxFileExists_AddsEdmxFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var edmxPath = Path.Combine(root, "LegacyModel.edmx");
            File.WriteAllText(edmxPath, "<edmx></edmx>");

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.EdmxObjectContext, finding.Category);
            Assert.Equal("LegacyModel.edmx", finding.Name);
            Assert.Equal(DataAccessSourceType.EdmxFile, finding.SourceType);
            Assert.Equal(edmxPath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("EDMX model file found.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.High, finding.Confidence);
            Assert.Contains("EDMX models", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenDbmlFileExists_AddsLinqToSqlFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var dbmlPath = Path.Combine(root, "LegacyModel.dbml");
            File.WriteAllText(dbmlPath, "<Database></Database>");

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.LinqToSql, finding.Category);
            Assert.Equal("LegacyModel.dbml", finding.Name);
            Assert.Equal(DataAccessSourceType.DbmlFile, finding.SourceType);
            Assert.Equal(dbmlPath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("LINQ to SQL DBML model file found.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.High, finding.Confidence);
            Assert.Contains("LINQ to SQL models", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenEfT4TemplateExists_AddsEdmxFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var t4Path = Path.Combine(root, "LegacyModel.Context.tt");
            File.WriteAllText(t4Path, "EntityFramework ObjectContext");

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.EdmxObjectContext, finding.Category);
            Assert.Equal("LegacyModel.Context.tt", finding.Name);
            Assert.Equal(DataAccessSourceType.T4Template, finding.SourceType);
            Assert.Equal(t4Path, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("EF-related T4 template found.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
            Assert.Contains("EF T4 templates", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenMigrationsFolderExists_AddsMigrationArtifactFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var migrationsPath = Path.Combine(root, "Migrations");
            Directory.CreateDirectory(migrationsPath);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.MigrationArtifact, finding.Category);
            Assert.Equal("Migrations", finding.Name);
            Assert.Equal(DataAccessSourceType.MigrationFolder, finding.SourceType);
            Assert.Equal(migrationsPath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("Migrations folder found.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
            Assert.Contains("Migration artifacts", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenRepositoryClassExists_AddsRepositoryPatternFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "CustomerRepository.cs");
            File.WriteAllText(
                sourcePath,
                """
                namespace SampleLegacyApp.Data;

                public sealed class CustomerRepository
                {
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.RepositoryPattern, finding.Category);
            Assert.Equal("CustomerRepository", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("Repository class or interface candidate found.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Low, finding.Confidence);
            Assert.Contains("Repository candidates", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenUnitOfWorkClassExists_AddsUnitOfWorkPatternFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "CustomerUnitOfWork.cs");
            File.WriteAllText(
                sourcePath,
                """
                namespace SampleLegacyApp.Data;

                public sealed class CustomerUnitOfWork
                {
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.UnitOfWorkPattern, finding.Category);
            Assert.Equal("CustomerUnitOfWork", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("Unit-of-work class or interface candidate found.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Low, finding.Confidence);
            Assert.Contains("Unit-of-work candidates", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenAdoNetTokensExist_AddsAdoNetFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "CustomerQuery.cs");
            File.WriteAllText(
                sourcePath,
                """
                using System.Data.SqlClient;

                namespace SampleLegacyApp.Data;

                public sealed class CustomerQuery
                {
                    public void Run()
                    {
                        using var connection = new SqlConnection();
                        using var command = new SqlCommand();
                    }
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.AdoNet, finding.Category);
            Assert.Equal("ADO.NET usage detected.", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("ADO.NET tokens such as SqlConnection, DbConnection, SqlCommand, or DbCommand were found.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
            Assert.Contains("Review connection management", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenDbContextTokensExist_AddsEfCoreFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "CustomerDbContext.cs");
            File.WriteAllText(
                sourcePath,
                """
                using Microsoft.EntityFrameworkCore;

                namespace SampleLegacyApp.Data;

                public sealed class CustomerDbContext : DbContext
                {
                    public DbSet<Customer> Customers => Set<Customer>();

                    protected override void OnModelCreating(ModelBuilder modelBuilder)
                    {
                    }
                }

                public sealed class Customer
                {
                    public int Id { get; set; }
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.EntityFrameworkCore, finding.Category);
            Assert.Equal("DbContext candidate detected.", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("DbContext token found in source.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
            Assert.Contains("Review DbContext", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenObjectContextTokensExist_AddsEdmxObjectContextFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "LegacyObjectContext.cs");
            File.WriteAllText(
                sourcePath,
                """
                using System.Data.Objects;

                namespace SampleLegacyApp.Data;

                public sealed class LegacyObjectContext : ObjectContext
                {
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.EdmxObjectContext, finding.Category);
            Assert.Equal("ObjectContext candidate detected.", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("ObjectContext token found in source.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.High, finding.Confidence);
            Assert.Contains("ObjectContext usage", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenDapperTokensExist_AddsDapperFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "DapperCustomerQuery.cs");
            File.WriteAllText(
                sourcePath,
                """
                using Dapper;

                namespace SampleLegacyApp.Data;

                public sealed class DapperCustomerQuery
                {
                    public void Run()
                    {
                        SqlMapper.Query<Customer>(null!, "SELECT * FROM Customers");
                    }
                }

                public sealed class Customer
                {
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            Assert.Contains(
                report.Findings,
                x => x.Category == DataAccessCategory.Dapper &&
                     x.Name == "Dapper usage detected." &&
                     x.SourcePath == sourcePath);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenNHibernateTokensExist_AddsNHibernateFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "CustomerSession.cs");
            File.WriteAllText(
                sourcePath,
                """
                using NHibernate;

                namespace SampleLegacyApp.Data;

                public sealed class CustomerSession
                {
                    private readonly ISession _session;

                    public CustomerSession(ISession session)
                    {
                        _session = session;
                    }
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.NHibernate, finding.Category);
            Assert.Equal("NHibernate usage detected.", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("NHibernate token found in source.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Medium, finding.Confidence);
            Assert.Contains("session factory", finding.MigrationConsideration, StringComparison.OrdinalIgnoreCase);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenStoredProcedureUsageExists_AddsStoredProcedureFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "StoredProcedureRunner.cs");
            File.WriteAllText(
                sourcePath,
                """
                using System.Data;

                namespace SampleLegacyApp.Data;

                public sealed class StoredProcedureRunner
                {
                    public CommandType CommandType => CommandType.StoredProcedure;
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.StoredProcedure, finding.Category);
            Assert.Equal("StoredProcedureRunner.cs", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("Possible stored procedure usage detected.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Low, finding.Confidence);
            Assert.Contains("Stored procedure indicators", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_WhenRawSqlUsageExists_AddsRawSqlFinding()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var sourcePath = Path.Combine(root, "RawSqlQuery.cs");
            File.WriteAllText(
                sourcePath,
                """
                namespace SampleLegacyApp.Data;

                public sealed class RawSqlQuery
                {
                    public string Sql => "SELECT * FROM Customers";
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            var finding = Assert.Single(report.Findings);

            Assert.Equal(DataAccessCategory.RawSql, finding.Category);
            Assert.Equal("RawSqlQuery.cs", finding.Name);
            Assert.Equal(DataAccessSourceType.SourceCode, finding.SourceType);
            Assert.Equal(sourcePath, finding.SourcePath);
            Assert.Equal(project.Name, finding.ProjectName);
            Assert.Equal("Possible raw SQL string detected.", finding.Evidence);
            Assert.Equal(DataAccessConfidence.Low, finding.Confidence);
            Assert.Contains("Raw SQL indicators", finding.MigrationConsideration);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_IgnoresBuildOutputFolders_WhenScanningProjectFiles()
    {
        var root = CreateTemporaryDirectory();

        try
        {
            var binDirectory = Path.Combine(root, "bin", "Debug");
            Directory.CreateDirectory(binDirectory);

            File.WriteAllText(
                Path.Combine(binDirectory, "GeneratedRepository.cs"),
                """
                namespace SampleLegacyApp.Data;

                public sealed class GeneratedRepository
                {
                }
                """);

            var project = CreateProject(root);

            var analyzer = new DataAccessAnalyzer();

            var report = analyzer.Analyze(
                new[] { project },
                Array.Empty<DiscoveredConfigFile>());

            Assert.Empty(report.Findings);
        }
        finally
        {
            DeleteDirectory(root);
        }
    }

    [Fact]
    public void Analyze_DeduplicatesFindings()
    {
        var project = new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferenceDetails =
            {
                new DiscoveredPackageReference
                {
                    Name = "Dapper",
                    Version = "2.1.35",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj"
                },
                new DiscoveredPackageReference
                {
                    Name = "Dapper",
                    Version = "2.1.35",
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj"
                }
            }
        };

        var analyzer = new DataAccessAnalyzer();

        var report = analyzer.Analyze(
            new[] { project },
            Array.Empty<DiscoveredConfigFile>());

        var finding = Assert.Single(report.Findings);

        Assert.Equal(DataAccessCategory.Dapper, finding.Category);
        Assert.Equal("Dapper", finding.Name);
    }

    private static DiscoveredProject CreateProject(string root)
    {
        return new DiscoveredProject
        {
            Name = "SampleLegacyApp.Data",
            ProjectFilePath = Path.Combine(root, "SampleLegacyApp.Data.csproj"),
            TargetFramework = "net48"
        };
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (!Directory.Exists(directory))
        {
            return;
        }

        try
        {
            Directory.Delete(directory, recursive: true);
        }
        catch
        {
            // Best effort cleanup only. The test result should not depend on temp directory deletion.
        }
    }
}