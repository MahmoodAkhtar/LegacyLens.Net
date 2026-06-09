using System.Text.RegularExpressions;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;

namespace LegacyLens.Core.Analysis;

public sealed class DataAccessAnalyzer
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public DataAccessInventoryReport Analyze(
        IReadOnlyCollection<DiscoveredProject> projects,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(configFiles);

        var findings = new List<DataAccessFinding>();

        AddConfigurationFindings(findings, configFiles);
        AddProjectPackageFindings(findings, projects);
        AddAssemblyReferenceFindings(findings, projects);
        AddProjectFileFindings(findings, projects);

        var distinctFindings = findings
            .GroupBy(CreateDeduplicationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(finding => GetCategoryPriority(finding.Category))
            .ThenBy(finding => finding.Category.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.ProjectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new DataAccessInventoryReport(distinctFindings);
    }

    private static void AddConfigurationFindings(
        ICollection<DataAccessFinding> findings,
        IEnumerable<DiscoveredConfigFile> configFiles)
    {
        foreach (var configFile in configFiles)
        {
            AddConnectionStringFindings(findings, configFile);
            AddProviderFindings(findings, configFile);
        }
    }

    private static void AddConnectionStringFindings(
        ICollection<DataAccessFinding> findings,
        DiscoveredConfigFile configFile)
    {
        foreach (var connectionString in configFile.ConnectionStrings)
        {
            findings.Add(new DataAccessFinding(
                DataAccessCategory.ConnectionString,
                connectionString.Name,
                DataAccessSourceType.Configuration,
                configFile.FilePath,
                ProjectName: null,
                Evidence: CreateConnectionStringEvidence(connectionString),
                MaskedValue: connectionString.MaskedConnectionString,
                DataAccessConfidence.High,
                MigrationConsideration: "Connection string should be verified by the development team before migration or environment setup."));
        }

        if (configFile.ConnectionStrings.Count == 0 && configFile.ConnectionStringsCount > 0)
        {
            findings.Add(new DataAccessFinding(
                DataAccessCategory.ConnectionString,
                GetFileNameOrFallback(configFile.FilePath, "Connection strings"),
                DataAccessSourceType.Configuration,
                configFile.FilePath,
                ProjectName: null,
                Evidence: $"{configFile.ConnectionStringsCount} connection string(s) configured.",
                MaskedValue: null,
                DataAccessConfidence.Medium,
                MigrationConsideration: "Connection string count may indicate database usage, but details were not available from the static configuration scan."));
        }
    }

    private static void AddProviderFindings(
        ICollection<DataAccessFinding> findings,
        DiscoveredConfigFile configFile)
    {
        foreach (var connectionString in configFile.ConnectionStrings)
        {
            if (string.IsNullOrWhiteSpace(connectionString.ProviderName))
            {
                continue;
            }

            findings.Add(new DataAccessFinding(
                DataAccessCategory.DatabaseProvider,
                connectionString.ProviderName,
                DataAccessSourceType.Configuration,
                configFile.FilePath,
                ProjectName: null,
                Evidence: $"Connection string providerName is {connectionString.ProviderName}.",
                MaskedValue: null,
                DataAccessConfidence.High,
                MigrationConsideration: CreateProviderMigrationConsideration(connectionString.ProviderName)));
        }
    }

    private static string CreateConnectionStringEvidence(DiscoveredConnectionString connectionString)
    {
        if (!string.IsNullOrWhiteSpace(connectionString.ProviderName))
        {
            return $"Connection string configured with provider {connectionString.ProviderName}.";
        }

        return "Connection string configured.";
    }

    private static string CreateProviderMigrationConsideration(string providerName)
    {
        if (providerName.Contains("SqlClient", StringComparison.OrdinalIgnoreCase))
        {
            return "SQL Server provider detected. Review provider package, connection-string format, authentication, and EF/Dapper/ADO.NET usage before migration.";
        }

        if (providerName.Contains("Npgsql", StringComparison.OrdinalIgnoreCase))
        {
            return "PostgreSQL provider detected. Review provider package, connection-string format, and ORM compatibility before migration.";
        }

        if (providerName.Contains("MySql", StringComparison.OrdinalIgnoreCase))
        {
            return "MySQL provider detected. Review provider package, connection-string format, and ORM compatibility before migration.";
        }

        if (providerName.Contains("Oracle", StringComparison.OrdinalIgnoreCase))
        {
            return "Oracle provider detected. Review provider package, connection-string format, and ORM compatibility before migration.";
        }

        return "Database provider should be reviewed before migration because provider packages and connection-string behaviour may differ on modern .NET.";
    }

    private static void AddProjectPackageFindings(
        ICollection<DataAccessFinding> findings,
        IEnumerable<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            AddDetailedPackageFindings(findings, project);
            AddPackageNameFindings(findings, project);
        }
    }

    private static void AddDetailedPackageFindings(
        ICollection<DataAccessFinding> findings,
        DiscoveredProject project)
    {
        foreach (var packageReference in project.PackageReferenceDetails)
        {
            var signal = ClassifyPackage(packageReference.Name);

            if (signal is null)
            {
                continue;
            }

            var version = string.IsNullOrWhiteSpace(packageReference.Version)
                ? "unknown version"
                : packageReference.Version;

            var sourcePath = FirstNonWhiteSpace(
                packageReference.SourcePath,
                project.ProjectFilePath);

            findings.Add(new DataAccessFinding(
                signal.Value.Category,
                packageReference.Name,
                DataAccessSourceType.PackageReference,
                sourcePath,
                project.Name,
                Evidence: $"{packageReference.Name} {version} package reference found from {packageReference.SourceFormat}.",
                MaskedValue: null,
                signal.Value.Confidence,
                MigrationConsideration: signal.Value.MigrationConsideration));
        }
    }

    private static void AddPackageNameFindings(
        ICollection<DataAccessFinding> findings,
        DiscoveredProject project)
    {
        var detailedPackageNames = project.PackageReferenceDetails
            .Select(package => package.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var packageName in project.PackageReferences)
        {
            if (string.IsNullOrWhiteSpace(packageName) || detailedPackageNames.Contains(packageName))
            {
                continue;
            }

            var signal = ClassifyPackage(packageName);

            if (signal is null)
            {
                continue;
            }

            findings.Add(new DataAccessFinding(
                signal.Value.Category,
                packageName,
                DataAccessSourceType.PackageReference,
                project.ProjectFilePath,
                project.Name,
                Evidence: $"{packageName} package reference found.",
                MaskedValue: null,
                signal.Value.Confidence,
                MigrationConsideration: signal.Value.MigrationConsideration));
        }
    }

    private static DataAccessSignal? ClassifyPackage(string packageName)
    {
        if (string.IsNullOrWhiteSpace(packageName))
        {
            return null;
        }

        if (packageName.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase))
        {
            return new DataAccessSignal(
                DataAccessCategory.EntityFramework6,
                DataAccessConfidence.High,
                "Classic Entity Framework detected. Review EF6 usage, EDMX/ObjectContext/DbContext patterns, and whether EF6 can remain isolated or needs migration.");
        }

        if (packageName.Equals("Microsoft.EntityFrameworkCore", StringComparison.OrdinalIgnoreCase) ||
            packageName.StartsWith("Microsoft.EntityFrameworkCore.", StringComparison.OrdinalIgnoreCase))
        {
            return new DataAccessSignal(
                DataAccessCategory.EntityFrameworkCore,
                DataAccessConfidence.High,
                "EF Core package detected. Review DbContext, migrations, provider packages, and target framework alignment.");
        }

        if (packageName.Equals("Dapper", StringComparison.OrdinalIgnoreCase))
        {
            return new DataAccessSignal(
                DataAccessCategory.Dapper,
                DataAccessConfidence.High,
                "Dapper package detected. Review raw SQL, stored procedure usage, connection management, and transaction boundaries.");
        }

        if (packageName.Equals("NHibernate", StringComparison.OrdinalIgnoreCase) ||
            packageName.StartsWith("NHibernate.", StringComparison.OrdinalIgnoreCase))
        {
            return new DataAccessSignal(
                DataAccessCategory.NHibernate,
                DataAccessConfidence.High,
                "NHibernate package detected. Review mappings, session factory configuration, transaction handling, and provider compatibility.");
        }

        if (IsDatabaseProviderPackage(packageName))
        {
            return new DataAccessSignal(
                DataAccessCategory.DatabaseProvider,
                DataAccessConfidence.Medium,
                "Database provider package detected. Review provider compatibility, connection-string format, authentication, and deployment requirements.");
        }

        if (packageName.Equals("System.Data.Linq", StringComparison.OrdinalIgnoreCase))
        {
            return new DataAccessSignal(
                DataAccessCategory.LinqToSql,
                DataAccessConfidence.High,
                "LINQ to SQL package detected. Review whether LINQ to SQL models need replacement or isolation during migration.");
        }

        return null;
    }

    private static bool IsDatabaseProviderPackage(string packageName)
    {
        return MatchesAny(
            packageName,
            "System.Data.SqlClient",
            "Microsoft.Data.SqlClient",
            "Npgsql",
            "MySql.Data",
            "MySqlConnector",
            "Oracle.ManagedDataAccess",
            "Oracle.ManagedDataAccess.Core",
            "Microsoft.EntityFrameworkCore.SqlServer",
            "Npgsql.EntityFrameworkCore.PostgreSQL",
            "Pomelo.EntityFrameworkCore.MySql",
            "Oracle.EntityFrameworkCore");
    }

    private static void AddAssemblyReferenceFindings(
        ICollection<DataAccessFinding> findings,
        IEnumerable<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            foreach (var assemblyReference in project.AssemblyReferences)
            {
                if (string.IsNullOrWhiteSpace(assemblyReference))
                {
                    continue;
                }

                var assemblyName = StripAssemblyMetadata(assemblyReference);
                var signal = ClassifyAssemblyReference(assemblyName);

                if (signal is null)
                {
                    continue;
                }

                findings.Add(new DataAccessFinding(
                    signal.Value.Category,
                    assemblyName,
                    DataAccessSourceType.AssemblyReference,
                    project.ProjectFilePath,
                    project.Name,
                    Evidence: $"{assemblyName} assembly reference found.",
                    MaskedValue: null,
                    signal.Value.Confidence,
                    MigrationConsideration: signal.Value.MigrationConsideration));
            }
        }
    }

    private static DataAccessSignal? ClassifyAssemblyReference(string assemblyName)
    {
        if (MatchesAny(
                assemblyName,
                "System.Data",
                "System.Data.Common",
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient"))
        {
            return new DataAccessSignal(
                DataAccessCategory.AdoNet,
                DataAccessConfidence.Medium,
                "ADO.NET-related assembly reference detected. Review direct connection, command, transaction, and data reader usage.");
        }

        if (MatchesAny(
                assemblyName,
                "Npgsql",
                "MySql.Data",
                "MySqlConnector",
                "Oracle.ManagedDataAccess"))
        {
            return new DataAccessSignal(
                DataAccessCategory.DatabaseProvider,
                DataAccessConfidence.Medium,
                "Database provider assembly reference detected. Review provider compatibility and deployment requirements.");
        }

        if (assemblyName.Equals("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
            assemblyName.StartsWith("EntityFramework.", StringComparison.OrdinalIgnoreCase))
        {
            return new DataAccessSignal(
                DataAccessCategory.EntityFramework6,
                DataAccessConfidence.High,
                "Entity Framework assembly reference detected. Review EF6 usage and migration or isolation strategy.");
        }

        if (assemblyName.Equals("System.Data.Linq", StringComparison.OrdinalIgnoreCase))
        {
            return new DataAccessSignal(
                DataAccessCategory.LinqToSql,
                DataAccessConfidence.High,
                "LINQ to SQL assembly reference detected. Review DBML models and replacement or isolation strategy.");
        }

        return null;
    }

    private static void AddProjectFileFindings(
        ICollection<DataAccessFinding> findings,
        IEnumerable<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);

            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            {
                continue;
            }

            AddEdmxAndModelFileFindings(findings, project, projectDirectory);
            AddMigrationFolderFindings(findings, project, projectDirectory);
            AddSourceCodeFindings(findings, project, projectDirectory);
        }
    }

    private static void AddEdmxAndModelFileFindings(
        ICollection<DataAccessFinding> findings,
        DiscoveredProject project,
        string projectDirectory)
    {
        foreach (var edmxFile in SafeEnumerateFiles(projectDirectory, "*.edmx"))
        {
            findings.Add(new DataAccessFinding(
                DataAccessCategory.EdmxObjectContext,
                Path.GetFileName(edmxFile),
                DataAccessSourceType.EdmxFile,
                edmxFile,
                project.Name,
                Evidence: "EDMX model file found.",
                MaskedValue: null,
                DataAccessConfidence.High,
                MigrationConsideration: "EDMX models are classic EF artifacts. Review ObjectContext/entities/mappings before planning EF Core migration."));
        }

        foreach (var dbmlFile in SafeEnumerateFiles(projectDirectory, "*.dbml"))
        {
            findings.Add(new DataAccessFinding(
                DataAccessCategory.LinqToSql,
                Path.GetFileName(dbmlFile),
                DataAccessSourceType.DbmlFile,
                dbmlFile,
                project.Name,
                Evidence: "LINQ to SQL DBML model file found.",
                MaskedValue: null,
                DataAccessConfidence.High,
                MigrationConsideration: "LINQ to SQL models require review because they do not directly map to EF Core migrations or DbContext scaffolding."));
        }

        foreach (var t4File in SafeEnumerateFiles(projectDirectory, "*.tt"))
        {
            if (!LooksLikeEfT4Template(t4File))
            {
                continue;
            }

            findings.Add(new DataAccessFinding(
                DataAccessCategory.EdmxObjectContext,
                Path.GetFileName(t4File),
                DataAccessSourceType.T4Template,
                t4File,
                project.Name,
                Evidence: "EF-related T4 template found.",
                MaskedValue: null,
                DataAccessConfidence.Medium,
                MigrationConsideration: "EF T4 templates may generate model or context code. Review generated-code dependency before migration."));
        }
    }

    private static bool LooksLikeEfT4Template(string filePath)
    {
        var fileName = Path.GetFileName(filePath);

        if (fileName.Contains("Context", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Model", StringComparison.OrdinalIgnoreCase) ||
            fileName.Contains("Entity", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var text = SafeReadAllText(filePath);

        return text.Contains("EntityFramework", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("ObjectContext", StringComparison.OrdinalIgnoreCase) ||
               text.Contains("DbContext", StringComparison.OrdinalIgnoreCase);
    }

    private static void AddMigrationFolderFindings(
        ICollection<DataAccessFinding> findings,
        DiscoveredProject project,
        string projectDirectory)
    {
        foreach (var directory in SafeEnumerateDirectories(projectDirectory))
        {
            if (!string.Equals(Path.GetFileName(directory), "Migrations", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            findings.Add(new DataAccessFinding(
                DataAccessCategory.MigrationArtifact,
                "Migrations",
                DataAccessSourceType.MigrationFolder,
                directory,
                project.Name,
                Evidence: "Migrations folder found.",
                MaskedValue: null,
                DataAccessConfidence.Medium,
                MigrationConsideration: "Migration artifacts should be reviewed to understand schema evolution history and EF migration strategy."));
        }
    }

    private static void AddSourceCodeFindings(
        ICollection<DataAccessFinding> findings,
        DiscoveredProject project,
        string projectDirectory)
    {
        foreach (var sourceFile in SafeEnumerateFiles(projectDirectory, "*.cs"))
        {
            var source = SafeReadAllText(sourceFile);

            if (string.IsNullOrWhiteSpace(source))
            {
                continue;
            }

            AddSourceTokenFinding(
                findings,
                project,
                sourceFile,
                source,
                DataAccessCategory.AdoNet,
                "ADO.NET usage detected.",
                "ADO.NET tokens such as SqlConnection, DbConnection, SqlCommand, or DbCommand were found.",
                "Review connection management, commands, transactions, and data reader usage before migration.",
                DataAccessConfidence.Medium,
                "SqlConnection",
                "SqlCommand",
                "DbConnection",
                "DbCommand",
                "IDbConnection",
                "IDataReader");

            AddSourceTokenFinding(
                findings,
                project,
                sourceFile,
                source,
                DataAccessCategory.EntityFrameworkCore,
                "DbContext candidate detected.",
                "DbContext token found in source.",
                "Review DbContext configuration, provider registration, migrations, and query behaviour before migration.",
                DataAccessConfidence.Medium,
                "DbContext",
                "DbSet<",
                "OnModelCreating");

            AddSourceTokenFinding(
                findings,
                project,
                sourceFile,
                source,
                DataAccessCategory.EdmxObjectContext,
                "ObjectContext candidate detected.",
                "ObjectContext token found in source.",
                "Review ObjectContext usage because EDMX/ObjectContext patterns usually need migration or isolation decisions.",
                DataAccessConfidence.High,
                "ObjectContext",
                "ObjectSet<",
                "EntityObject");

            AddSourceTokenFinding(
                findings,
                project,
                sourceFile,
                source,
                DataAccessCategory.Dapper,
                "Dapper usage detected.",
                "Dapper token or common Dapper call found in source.",
                "Review SQL strings, parameter handling, connection lifetimes, and transaction boundaries.",
                DataAccessConfidence.Medium,
                "using Dapper",
                "SqlMapper",
                ".Query<",
                ".QueryAsync",
                ".ExecuteAsync");

            AddSourceTokenFinding(
                findings,
                project,
                sourceFile,
                source,
                DataAccessCategory.NHibernate,
                "NHibernate usage detected.",
                "NHibernate token found in source.",
                "Review session factory, mappings, transactions, and provider compatibility.",
                DataAccessConfidence.Medium,
                "NHibernate",
                "ISession",
                "SessionFactory");

            if (LooksLikeRepositoryCandidate(sourceFile, source))
            {
                findings.Add(new DataAccessFinding(
                    DataAccessCategory.RepositoryPattern,
                    Path.GetFileNameWithoutExtension(sourceFile),
                    DataAccessSourceType.SourceCode,
                    sourceFile,
                    project.Name,
                    Evidence: "Repository class or interface candidate found.",
                    MaskedValue: null,
                    DataAccessConfidence.Low,
                    MigrationConsideration: "Repository candidates should be reviewed to understand persistence boundaries and whether queries are centralised or spread through the application."));
            }

            if (LooksLikeUnitOfWorkCandidate(sourceFile, source))
            {
                findings.Add(new DataAccessFinding(
                    DataAccessCategory.UnitOfWorkPattern,
                    Path.GetFileNameWithoutExtension(sourceFile),
                    DataAccessSourceType.SourceCode,
                    sourceFile,
                    project.Name,
                    Evidence: "Unit-of-work class or interface candidate found.",
                    MaskedValue: null,
                    DataAccessConfidence.Low,
                    MigrationConsideration: "Unit-of-work candidates should be reviewed to understand transaction boundaries and persistence orchestration."));
            }

            if (LooksLikeStoredProcedureUsage(source))
            {
                findings.Add(new DataAccessFinding(
                    DataAccessCategory.StoredProcedure,
                    Path.GetFileName(sourceFile),
                    DataAccessSourceType.SourceCode,
                    sourceFile,
                    project.Name,
                    Evidence: "Possible stored procedure usage detected.",
                    MaskedValue: null,
                    DataAccessConfidence.Low,
                    MigrationConsideration: "Stored procedure indicators should be verified by the development team before changing data access code or schema deployment."));
            }

            if (LooksLikeRawSqlUsage(source))
            {
                findings.Add(new DataAccessFinding(
                    DataAccessCategory.RawSql,
                    Path.GetFileName(sourceFile),
                    DataAccessSourceType.SourceCode,
                    sourceFile,
                    project.Name,
                    Evidence: "Possible raw SQL string detected.",
                    MaskedValue: null,
                    DataAccessConfidence.Low,
                    MigrationConsideration: "Raw SQL indicators should be reviewed for SQL dialect, parameter handling, stored procedure calls, and provider compatibility."));
            }
        }
    }

    private static void AddSourceTokenFinding(
        ICollection<DataAccessFinding> findings,
        DiscoveredProject project,
        string sourceFile,
        string source,
        DataAccessCategory category,
        string name,
        string evidence,
        string migrationConsideration,
        DataAccessConfidence confidence,
        params string[] tokens)
    {
        if (!tokens.Any(token => source.Contains(token, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        findings.Add(new DataAccessFinding(
            category,
            name,
            DataAccessSourceType.SourceCode,
            sourceFile,
            project.Name,
            Evidence: evidence,
            MaskedValue: null,
            confidence,
            MigrationConsideration: migrationConsideration));
    }

    private static bool LooksLikeRepositoryCandidate(string sourceFile, string source)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourceFile);

        if (fileName.EndsWith("Repository", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Regex.IsMatch(source, @"\b(class|interface)\s+I?\w*Repository\b");
    }

    private static bool LooksLikeUnitOfWorkCandidate(string sourceFile, string source)
    {
        var fileName = Path.GetFileNameWithoutExtension(sourceFile);

        if (fileName.Contains("UnitOfWork", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return Regex.IsMatch(source, @"\b(class|interface)\s+I?\w*UnitOfWork\b");
    }

    private static bool LooksLikeStoredProcedureUsage(string source)
    {
        return source.Contains("CommandType.StoredProcedure", StringComparison.OrdinalIgnoreCase) ||
               Regex.IsMatch(source, @"(?i)\bEXEC(?:UTE)?\s+\w");
    }

    private static bool LooksLikeRawSqlUsage(string source)
    {
        return Regex.IsMatch(
            source,
            @"(?i)(""|@""|\$""|\$@"")[^""\r\n]*(SELECT|INSERT|UPDATE|DELETE|MERGE)\s+");
    }

    private static IEnumerable<string> SafeEnumerateFiles(string directory, string searchPattern)
    {
        try
        {
            return Directory
                .EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories)
                .Where(path => !IsBuildOutputPath(path))
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static IEnumerable<string> SafeEnumerateDirectories(string directory)
    {
        try
        {
            return Directory
                .EnumerateDirectories(directory, "*", SearchOption.AllDirectories)
                .Where(path => !IsBuildOutputPath(path))
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool IsBuildOutputPath(string path)
    {
        var parts = path.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase));
    }

    private static string SafeReadAllText(string path)
    {
        try
        {
            return File.ReadAllText(path);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static string StripAssemblyMetadata(string assemblyReference)
    {
        var commaIndex = assemblyReference.IndexOf(',', StringComparison.Ordinal);

        return commaIndex < 0
            ? assemblyReference.Trim()
            : assemblyReference[..commaIndex].Trim();
    }

    private static bool MatchesAny(string value, params string[] candidates)
    {
        return candidates.Any(candidate => Comparer.Equals(value, candidate));
    }

    private static string GetFileNameOrFallback(string? path, string fallback)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return fallback;
        }

        var fileName = Path.GetFileName(path);

        return string.IsNullOrWhiteSpace(fileName)
            ? fallback
            : fileName;
    }

    private static string FirstNonWhiteSpace(params string?[] values)
    {
        foreach (var value in values)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return string.Empty;
    }

    private static string CreateDeduplicationKey(DataAccessFinding finding)
    {
        return string.Join(
            "|",
            finding.Category,
            finding.Name,
            finding.SourceType,
            finding.SourcePath,
            finding.ProjectName ?? string.Empty,
            finding.Evidence);
    }

    private static int GetCategoryPriority(DataAccessCategory category)
    {
        return category switch
        {
            DataAccessCategory.ConnectionString => 10,
            DataAccessCategory.DatabaseProvider => 20,
            DataAccessCategory.EntityFramework6 => 30,
            DataAccessCategory.EntityFrameworkCore => 40,
            DataAccessCategory.EdmxObjectContext => 50,
            DataAccessCategory.LinqToSql => 60,
            DataAccessCategory.AdoNet => 70,
            DataAccessCategory.Dapper => 80,
            DataAccessCategory.NHibernate => 90,
            DataAccessCategory.RawSql => 100,
            DataAccessCategory.StoredProcedure => 110,
            DataAccessCategory.RepositoryPattern => 120,
            DataAccessCategory.UnitOfWorkPattern => 130,
            DataAccessCategory.MigrationArtifact => 140,
            DataAccessCategory.UnknownRequiresReview => 999,
            _ => 999
        };
    }

    private readonly record struct DataAccessSignal(
        DataAccessCategory Category,
        DataAccessConfidence Confidence,
        string MigrationConsideration);
}