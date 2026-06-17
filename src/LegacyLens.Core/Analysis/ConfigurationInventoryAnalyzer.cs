using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace LegacyLens.Core.Analysis;

public sealed class ConfigurationInventoryAnalyzer
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public ConfigurationInventoryReport Analyze(
        IReadOnlyCollection<DiscoveredProject> projects,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(configFiles);

        var fileInventory = new ScanFileInventoryBuilder().Build(projects);

        return Analyze(
            projects,
            configFiles,
            fileInventory);
    }

    public ConfigurationInventoryReport Analyze(
        IReadOnlyCollection<DiscoveredProject> projects,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles,
        ScanFileInventory fileInventory)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(configFiles);
        ArgumentNullException.ThrowIfNull(fileInventory);

        var findings = new List<ConfigurationInventoryFinding>();

        AddDiscoveredConfigurationFindings(findings, projects, configFiles);
        AddPhysicalConfigurationFileFindings(findings, projects);
        AddConfigurationApiUsageFindings(findings, fileInventory);

        var distinctFindings = findings
            .GroupBy(CreateDeduplicationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(finding => GetCategoryPriority(finding.Category))
            .ThenBy(finding => finding.Category.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.ProjectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(finding => finding.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var sourceUsages = DiscoverSourceConfigurationUsages(fileInventory)
            .GroupBy(CreateUsageDeduplicationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(usage => usage.ProjectName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(usage => usage.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ThenBy(usage => usage.LineNumber)
            .ThenBy(usage => usage.Kind)
            .ThenBy(usage => usage.Key ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        var configuredKeys = DiscoverConfiguredKeys(distinctFindings);
        var resolvedSourceUsages = ResolveSourceUsages(sourceUsages, configuredKeys);
        var reconciliations = CreateKeyReconciliations(configuredKeys, resolvedSourceUsages);

        return new ConfigurationInventoryReport(
            distinctFindings,
            resolvedSourceUsages,
            reconciliations);
    }

    private static void AddDiscoveredConfigurationFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        IReadOnlyCollection<DiscoveredProject> projects,
        IEnumerable<DiscoveredConfigFile> configFiles)
    {
        foreach (var configFile in configFiles)
        {
            var projectName = ResolveProjectName(projects, configFile.FilePath);

            AddConfigurationFileFinding(findings, configFile, projectName);
            AddAppSettingFindings(findings, configFile, projectName);
            AddConnectionStringFindings(findings, configFile, projectName);
            AddCustomSectionFindings(findings, configFile, projectName);
            AddXmlSectionFindings(findings, configFile, projectName);
        }
    }

    private static string? ResolveProjectName(
        IEnumerable<DiscoveredProject> projects,
        string sourcePath)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            return null;
        }

        var fullSourcePath = Path.GetFullPath(sourcePath);

        return projects
            .Select(project => new
            {
                Project = project,
                ProjectDirectory = Path.GetDirectoryName(project.ProjectFilePath)
            })
            .Where(candidate => !string.IsNullOrWhiteSpace(candidate.ProjectDirectory))
            .Select(candidate => new
            {
                candidate.Project.Name,
                ProjectDirectory = Path.GetFullPath(candidate.ProjectDirectory!)
            })
            .Where(candidate => IsSameOrChildPath(fullSourcePath, candidate.ProjectDirectory))
            .OrderByDescending(candidate => candidate.ProjectDirectory.Length)
            .Select(candidate => candidate.Name)
            .FirstOrDefault();
    }

    private static bool IsSameOrChildPath(string path, string candidateDirectory)
    {
        var normalisedPath = Path.GetFullPath(path)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        var normalisedDirectory = Path.GetFullPath(candidateDirectory)
            .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return normalisedPath.Equals(normalisedDirectory, StringComparison.OrdinalIgnoreCase) ||
               normalisedPath.StartsWith(
                   normalisedDirectory + Path.DirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase) ||
               normalisedPath.StartsWith(
                   normalisedDirectory + Path.AltDirectorySeparatorChar,
                   StringComparison.OrdinalIgnoreCase);
    }

    private static void AddConfigurationFileFinding(
        ICollection<ConfigurationInventoryFinding> findings,
        DiscoveredConfigFile configFile,
        string? projectName)
    {
        findings.Add(new ConfigurationInventoryFinding(
            ConfigurationInventoryCategory.ConfigurationFile,
            GetFileNameOrFallback(configFile.FilePath, "Configuration file"),
            ConfigurationInventorySourceType.Configuration,
            configFile.FilePath,
            ProjectName: projectName,
            Evidence: CreateConfigurationFileEvidence(configFile),
            MaskedValue: null,
            ConfigurationInventoryConfidence.High,
            RequiresReview: true,
            MigrationConsideration: "Review this configuration file before migration because runtime configuration may need mapping to appsettings.json, environment variables, options binding, hosting configuration, or deployment settings."));
    }

    private static string CreateConfigurationFileEvidence(DiscoveredConfigFile configFile)
    {
        return $"{configFile.AppSettingsCount} app setting(s), {configFile.ConnectionStringsCount} connection string(s), and {configFile.CustomSectionCount} custom section(s) discovered.";
    }

    private static void AddAppSettingFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        DiscoveredConfigFile configFile,
        string? projectName)
    {
        foreach (var appSetting in configFile.AppSettings)
        {
            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.AppSetting,
                appSetting.Key,
                ConfigurationInventorySourceType.Configuration,
                configFile.FilePath,
                ProjectName: projectName,
                Evidence: "App setting configured.",
                MaskedValue: MaskSensitiveValue(appSetting.MaskedValue, appSetting.Key),
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review whether this app setting should move to appsettings.json, environment-specific configuration, user secrets, Key Vault, or another configuration provider."));
        }

        if (configFile.AppSettings.Count == 0 && configFile.AppSettingsCount > 0)
        {
            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.AppSetting,
                GetFileNameOrFallback(configFile.FilePath, "App settings"),
                ConfigurationInventorySourceType.Configuration,
                configFile.FilePath,
                ProjectName: projectName,
                Evidence: $"{configFile.AppSettingsCount} app setting(s) configured.",
                MaskedValue: null,
                ConfigurationInventoryConfidence.Medium,
                RequiresReview: true,
                MigrationConsideration: "App setting count indicates visible configuration surface, but individual keys were not available from the static configuration scan."));
        }
    }

    private static void AddConnectionStringFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        DiscoveredConfigFile configFile,
        string? projectName)
    {
        foreach (var connectionString in configFile.ConnectionStrings)
        {
            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.ConnectionString,
                connectionString.Name,
                ConfigurationInventorySourceType.Configuration,
                configFile.FilePath,
                ProjectName: projectName,
                Evidence: CreateConnectionStringEvidence(connectionString),
                MaskedValue: MaskSensitiveValue(connectionString.MaskedConnectionString, connectionString.Name),
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review connection string storage, provider compatibility, deployment substitution, and secret handling before migration."));
        }

        if (configFile.ConnectionStrings.Count == 0 && configFile.ConnectionStringsCount > 0)
        {
            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.ConnectionString,
                GetFileNameOrFallback(configFile.FilePath, "Connection strings"),
                ConfigurationInventorySourceType.Configuration,
                configFile.FilePath,
                ProjectName: projectName,
                Evidence: $"{configFile.ConnectionStringsCount} connection string(s) configured.",
                MaskedValue: null,
                ConfigurationInventoryConfidence.Medium,
                RequiresReview: true,
                MigrationConsideration: "Connection string count indicates possible runtime data configuration, but individual connection strings were not available from the static configuration scan."));
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

    private static void AddCustomSectionFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        DiscoveredConfigFile configFile,
        string? projectName)
    {
        foreach (var section in configFile.CustomSections)
        {
            var evidence = string.IsNullOrWhiteSpace(section.Type)
                ? "Custom configuration section declared."
                : $"Custom configuration section declared with type {section.Type}.";

            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.CustomSection,
                section.Name,
                ConfigurationInventorySourceType.Configuration,
                configFile.FilePath,
                ProjectName: projectName,
                Evidence: evidence,
                MaskedValue: null,
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review custom configuration sections because they may need replacement with options binding, custom configuration providers, or explicit migration code."));
        }

        if (configFile.CustomSections.Count == 0 && configFile.CustomSectionCount > 0)
        {
            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.CustomSection,
                GetFileNameOrFallback(configFile.FilePath, "Custom sections"),
                ConfigurationInventorySourceType.Configuration,
                configFile.FilePath,
                ProjectName: projectName,
                Evidence: $"{configFile.CustomSectionCount} custom configuration section(s) declared.",
                MaskedValue: null,
                ConfigurationInventoryConfidence.Medium,
                RequiresReview: true,
                MigrationConsideration: "Custom section count indicates configuration extension points, but individual section names were not available from the static configuration scan."));
        }
    }

    private static void AddXmlSectionFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        DiscoveredConfigFile configFile,
        string? projectName)
    {
        var document = TryLoadXml(configFile.FilePath);

        if (document is null)
        {
            return;
        }

        if (HasElement(document, "system.serviceModel"))
        {
            AddXmlSectionFinding(
                findings,
                ConfigurationInventoryCategory.WcfConfiguration,
                "system.serviceModel",
                configFile.FilePath,
                projectName,
                "WCF system.serviceModel configuration section found.",
                "Review WCF service/client endpoints, bindings, behaviours, security, timeouts, quotas, and migration approach before moving to modern .NET.");
        }

        if (HasElement(document, "system.web") || HasElement(document, "system.webServer"))
        {
            AddXmlSectionFinding(
                findings,
                ConfigurationInventoryCategory.AspNetIisConfiguration,
                "ASP.NET / IIS configuration",
                configFile.FilePath,
                projectName,
                "system.web or system.webServer configuration section found.",
                "Review classic ASP.NET and IIS configuration because ASP.NET Core uses a different hosting and middleware configuration model.");
        }

        if (HasElement(document, "bindingRedirect"))
        {
            AddXmlSectionFinding(
                findings,
                ConfigurationInventoryCategory.BindingRedirect,
                "bindingRedirect",
                configFile.FilePath,
                projectName,
                "Assembly binding redirect found.",
                "Review binding redirects because modern .NET dependency resolution differs from .NET Framework assembly binding policy.");
        }

        if (HasElement(document, "authentication") || HasElement(document, "authorization"))
        {
            AddXmlSectionFinding(
                findings,
                ConfigurationInventoryCategory.AuthenticationAuthorization,
                "authentication / authorization",
                configFile.FilePath,
                projectName,
                "Authentication or authorization configuration section found.",
                "Review authentication and authorization configuration because middleware, policies, schemes, and hosting integration may need migration.");
        }

        if (HasElement(document, "log4net") ||
            HasElement(document, "nlog") ||
            HasElement(document, "serilog") ||
            HasElement(document, "system.diagnostics"))
        {
            AddXmlSectionFinding(
                findings,
                ConfigurationInventoryCategory.LoggingDiagnostics,
                "logging / diagnostics",
                configFile.FilePath,
                projectName,
                "Logging or diagnostics configuration section found.",
                "Review logging and diagnostics configuration before migration because providers, sinks, filters, and hosting integration may change.");
        }

        if (HasElement(document, "entityFramework"))
        {
            AddXmlSectionFinding(
                findings,
                ConfigurationInventoryCategory.EntityFrameworkConfiguration,
                "entityFramework",
                configFile.FilePath,
                projectName,
                "Entity Framework configuration section found.",
                "Review Entity Framework configuration, provider registrations, connection factories, and EF6/EF Core migration decisions.");
        }

        if (HasElement(document, "mailSettings") || HasElement(document, "smtp"))
        {
            AddXmlSectionFinding(
                findings,
                ConfigurationInventoryCategory.SmtpMail,
                "SMTP / mailSettings",
                configFile.FilePath,
                projectName,
                "SMTP or mailSettings configuration section found.",
                "Review SMTP host, credentials, delivery method, and secret handling before migration or local environment setup.");
        }
    }

    private static void AddXmlSectionFinding(
        ICollection<ConfigurationInventoryFinding> findings,
        ConfigurationInventoryCategory category,
        string name,
        string sourcePath,
        string? projectName,
        string evidence,
        string migrationConsideration)
    {
        findings.Add(new ConfigurationInventoryFinding(
            category,
            name,
            ConfigurationInventorySourceType.Configuration,
            sourcePath,
            ProjectName: projectName,
            Evidence: evidence,
            MaskedValue: null,
            ConfigurationInventoryConfidence.High,
            RequiresReview: true,
            MigrationConsideration: migrationConsideration));
    }

    private static XDocument? TryLoadXml(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return null;
        }

        try
        {
            return XDocument.Load(path);
        }
        catch
        {
            return null;
        }
    }

    private static bool HasElement(XDocument document, string localName)
    {
        return document
            .Descendants()
            .Any(element => element.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));
    }

    private static void AddPhysicalConfigurationFileFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        IEnumerable<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            var projectDirectory = Path.GetDirectoryName(project.ProjectFilePath);

            if (string.IsNullOrWhiteSpace(projectDirectory) || !Directory.Exists(projectDirectory))
            {
                continue;
            }

            foreach (var path in EnumerateFiles(projectDirectory, "*.config"))
            {
                AddPhysicalConfigurationFileFinding(findings, project, path);
            }

            foreach (var path in EnumerateFiles(projectDirectory, "appsettings*.json"))
            {
                AddJsonConfigurationFindings(findings, project, path);
            }

            foreach (var path in EnumerateFiles(projectDirectory, "*.settings"))
            {
                findings.Add(new ConfigurationInventoryFinding(
                    ConfigurationInventoryCategory.SettingsFile,
                    Path.GetFileName(path),
                    ConfigurationInventorySourceType.SettingsFile,
                    path,
                    project.Name,
                    ".settings file found.",
                    MaskedValue: null,
                    ConfigurationInventoryConfidence.High,
                    RequiresReview: true,
                    MigrationConsideration: "Review .settings usage because strongly typed settings and generated settings classes may need migration to options binding or another configuration model."));
            }

            foreach (var path in EnumerateFiles(projectDirectory, "NuGet.config"))
            {
                findings.Add(new ConfigurationInventoryFinding(
                    ConfigurationInventoryCategory.BuildPackageConfiguration,
                    Path.GetFileName(path),
                    ConfigurationInventorySourceType.NuGetConfig,
                    path,
                    project.Name,
                    "NuGet.config file found.",
                    MaskedValue: null,
                    ConfigurationInventoryConfidence.High,
                    RequiresReview: true,
                    MigrationConsideration: "Review package sources, private feeds, credentials, and CI restore configuration before migration or onboarding."));
            }
        }
    }

    private static void AddJsonConfigurationFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        DiscoveredProject project,
        string path)
    {
        findings.Add(new ConfigurationInventoryFinding(
            ConfigurationInventoryCategory.JsonConfiguration,
            Path.GetFileName(path),
            ConfigurationInventorySourceType.JsonConfiguration,
            path,
            project.Name,
            "JSON application configuration file found.",
            MaskedValue: null,
            ConfigurationInventoryConfidence.High,
            RequiresReview: true,
            MigrationConsideration: "Review JSON configuration values, environment-specific overrides, deployment substitutions, and secret handling."));

        foreach (var setting in DiscoverJsonSettings(path))
        {
            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.JsonConfiguration,
                setting.Name,
                ConfigurationInventorySourceType.JsonConfiguration,
                path,
                project.Name,
                "JSON setting configured.",
                MaskSensitiveValue(setting.Value, setting.Name),
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review JSON configuration values, options binding, environment-specific overrides, deployment substitutions, and secret handling."));
        }
    }

    private static IReadOnlyList<JsonSetting> DiscoverJsonSettings(string path)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
        {
            return Array.Empty<JsonSetting>();
        }

        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));

            var settings = new List<JsonSetting>();
            AddJsonSettings(settings, document.RootElement, prefix: null);

            return settings
                .OrderBy(setting => setting.Name, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<JsonSetting>();
        }
    }

    private static void AddJsonSettings(
        ICollection<JsonSetting> settings,
        JsonElement element,
        string? prefix)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var property in element.EnumerateObject())
                {
                    var name = string.IsNullOrWhiteSpace(prefix)
                        ? property.Name
                        : $"{prefix}:{property.Name}";

                    AddJsonSettings(settings, property.Value, name);
                }

                break;

            case JsonValueKind.Array:
                var index = 0;

                foreach (var item in element.EnumerateArray())
                {
                    var name = string.IsNullOrWhiteSpace(prefix)
                        ? $"[{index}]"
                        : $"{prefix}:{index}";

                    AddJsonSettings(settings, item, name);
                    index++;
                }

                break;

            case JsonValueKind.String:
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    settings.Add(new JsonSetting(prefix, element.GetString()));
                }

                break;

            case JsonValueKind.Number:
            case JsonValueKind.True:
            case JsonValueKind.False:
            case JsonValueKind.Null:
                if (!string.IsNullOrWhiteSpace(prefix))
                {
                    settings.Add(new JsonSetting(prefix, element.ToString()));
                }

                break;
        }
    }

    private static void AddPhysicalConfigurationFileFinding(
        ICollection<ConfigurationInventoryFinding> findings,
        DiscoveredProject project,
        string path)
    {
        var fileName = Path.GetFileName(path);

        if (IsEnvironmentTransform(path))
        {
            findings.Add(new ConfigurationInventoryFinding(
                ConfigurationInventoryCategory.EnvironmentTransform,
                fileName,
                ConfigurationInventorySourceType.Transform,
                path,
                project.Name,
                "Environment-specific configuration transform file found.",
                MaskedValue: null,
                ConfigurationInventoryConfidence.High,
                RequiresReview: true,
                MigrationConsideration: "Review transform behaviour manually because this static analysis does not apply transforms or evaluate deployment-time substitutions."));

            return;
        }

        findings.Add(new ConfigurationInventoryFinding(
            ConfigurationInventoryCategory.ConfigurationFile,
            fileName,
            ConfigurationInventorySourceType.Configuration,
            path,
            project.Name,
            "Configuration file found in project directory.",
            MaskedValue: null,
            ConfigurationInventoryConfidence.Medium,
            RequiresReview: true,
            MigrationConsideration: "Review this configuration file for runtime settings, environment differences, and migration to modern configuration providers."));
    }

    private static bool IsEnvironmentTransform(string path)
    {
        var fileName = Path.GetFileName(path);

        return fileName.StartsWith("Web.", StringComparison.OrdinalIgnoreCase) &&
               fileName.EndsWith(".config", StringComparison.OrdinalIgnoreCase) &&
               !fileName.Equals("Web.config", StringComparison.OrdinalIgnoreCase);
    }

    private static IReadOnlyList<string> EnumerateFiles(string directory, string searchPattern)
    {
        try
        {
            return Directory
                .EnumerateFiles(directory, searchPattern, SearchOption.AllDirectories)
                .Where(path => !IsExcludedPath(path))
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    private static bool IsExcludedPath(string path)
    {
        var parts = path.Split(
            [Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar],
            StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Any(part =>
            part.Equals("bin", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("obj", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("output", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("reports", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("artifacts", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Debug", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Release", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Log", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("Logs", StringComparison.OrdinalIgnoreCase) ||
            part.Equals("CodeCoverage", StringComparison.OrdinalIgnoreCase) ||
            part.StartsWith("TestResult", StringComparison.OrdinalIgnoreCase));
    }

    private static void AddConfigurationApiUsageFindings(
        ICollection<ConfigurationInventoryFinding> findings,
        ScanFileInventory fileInventory)
    {
        foreach (var sourceFile in fileInventory.CSharpFiles)
        {
            if (string.IsNullOrWhiteSpace(sourceFile.Content))
            {
                continue;
            }

            foreach (var apiUsage in DiscoverConfigurationApiUsages(sourceFile.Content))
            {
                findings.Add(new ConfigurationInventoryFinding(
                    ConfigurationInventoryCategory.ConfigurationApiUsage,
                    apiUsage.Name,
                    ConfigurationInventorySourceType.SourceCode,
                    sourceFile.FullPath,
                    sourceFile.ProjectName,
                    apiUsage.Evidence,
                    MaskedValue: null,
                    ConfigurationInventoryConfidence.High,
                    RequiresReview: true,
                    MigrationConsideration: apiUsage.MigrationConsideration));
            }
        }
    }

    private static IEnumerable<ConfigurationApiUsage> DiscoverConfigurationApiUsages(string content)
    {
        if (content.Contains("ConfigurationManager.AppSettings", StringComparison.Ordinal))
        {
            yield return new ConfigurationApiUsage(
                "ConfigurationManager.AppSettings",
                "ConfigurationManager.AppSettings usage found.",
                "Review migration to IConfiguration, options binding, or an explicit configuration abstraction.");
        }

        if (content.Contains("ConfigurationManager.ConnectionStrings", StringComparison.Ordinal))
        {
            yield return new ConfigurationApiUsage(
                "ConfigurationManager.ConnectionStrings",
                "ConfigurationManager.ConnectionStrings usage found.",
                "Review connection string access and secret handling in the modern configuration model.");
        }

        if (Regex.IsMatch(content, @"\bIConfiguration\b", RegexOptions.CultureInvariant))
        {
            yield return new ConfigurationApiUsage(
                "IConfiguration",
                "IConfiguration usage found.",
                "Review configuration provider registration, binding, validation, and environment-specific configuration behaviour.");
        }

        if (Regex.IsMatch(content, @"\.GetSection\s*\(", RegexOptions.CultureInvariant) ||
            Regex.IsMatch(content, @"\bGetSection\s*\(", RegexOptions.CultureInvariant))
        {
            yield return new ConfigurationApiUsage(
                "GetSection",
                "GetSection usage found.",
                "Review named configuration sections and options binding during migration.");
        }
    }

    private static IReadOnlyList<ConfigurationUsageFinding> DiscoverSourceConfigurationUsages(
        ScanFileInventory fileInventory)
    {
        var usages = new List<ConfigurationUsageFinding>();

        foreach (var sourceFile in fileInventory.CSharpFiles)
        {
            if (string.IsNullOrWhiteSpace(sourceFile.Content))
            {
                continue;
            }

            SyntaxTree syntaxTree;
            CompilationUnitSyntax root;

            try
            {
                syntaxTree = CSharpSyntaxTree.ParseText(sourceFile.Content);
                root = syntaxTree.GetCompilationUnitRoot();
            }
            catch
            {
                continue;
            }

            usages.AddRange(DiscoverElementAccessConfigurationUsages(sourceFile, syntaxTree, root));
            usages.AddRange(DiscoverGetMethodConfigurationUsages(sourceFile, syntaxTree, root));
        }

        return usages;
    }

    private static IEnumerable<ConfigurationUsageFinding> DiscoverElementAccessConfigurationUsages(
        ScanFile sourceFile,
        SyntaxTree syntaxTree,
        CompilationUnitSyntax root)
    {
        foreach (var elementAccess in root.DescendantNodes().OfType<ElementAccessExpressionSyntax>())
        {
            if (!TryParseConfigurationCollection(elementAccess.Expression, out var kind))
            {
                continue;
            }

            var argument = elementAccess.ArgumentList.Arguments.FirstOrDefault()?.Expression;

            yield return CreateSourceUsage(
                sourceFile,
                syntaxTree,
                elementAccess,
                kind,
                ExtractLiteralString(argument));
        }
    }

    private static IEnumerable<ConfigurationUsageFinding> DiscoverGetMethodConfigurationUsages(
        ScanFile sourceFile,
        SyntaxTree syntaxTree,
        CompilationUnitSyntax root)
    {
        foreach (var invocation in root.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (invocation.Expression is not MemberAccessExpressionSyntax memberAccess ||
                !memberAccess.Name.Identifier.Text.Equals("Get", StringComparison.Ordinal) ||
                !TryParseConfigurationCollection(memberAccess.Expression, out var kind))
            {
                continue;
            }

            var argument = invocation.ArgumentList.Arguments.FirstOrDefault()?.Expression;

            yield return CreateSourceUsage(
                sourceFile,
                syntaxTree,
                invocation,
                kind,
                ExtractLiteralString(argument));
        }
    }

    private static bool TryParseConfigurationCollection(
        ExpressionSyntax expression,
        out ConfigurationUsageKind kind)
    {
        kind = default;

        if (expression is not MemberAccessExpressionSyntax memberAccess)
        {
            return false;
        }

        if (!IsConfigurationManagerExpression(memberAccess.Expression))
        {
            return false;
        }

        if (memberAccess.Name.Identifier.Text.Equals("AppSettings", StringComparison.Ordinal))
        {
            kind = ConfigurationUsageKind.AppSetting;
            return true;
        }

        if (memberAccess.Name.Identifier.Text.Equals("ConnectionStrings", StringComparison.Ordinal))
        {
            kind = ConfigurationUsageKind.ConnectionString;
            return true;
        }

        return false;
    }

    private static bool IsConfigurationManagerExpression(ExpressionSyntax expression)
    {
        var text = expression.ToString();

        return text.Equals("ConfigurationManager", StringComparison.Ordinal) ||
               text.EndsWith(".ConfigurationManager", StringComparison.Ordinal);
    }

    private static string? ExtractLiteralString(ExpressionSyntax? expression)
    {
        return expression is LiteralExpressionSyntax literal &&
               literal.IsKind(SyntaxKind.StringLiteralExpression)
            ? literal.Token.ValueText
            : null;
    }

    private static ConfigurationUsageFinding CreateSourceUsage(
        ScanFile sourceFile,
        SyntaxTree syntaxTree,
        SyntaxNode node,
        ConfigurationUsageKind kind,
        string? literalKey)
    {
        var lineNumber = syntaxTree.GetLineSpan(node.Span).StartLinePosition.Line + 1;
        var requiresReview = string.IsNullOrWhiteSpace(literalKey);
        var resolution = requiresReview
            ? ConfigurationUsageKeyResolution.DynamicKeyRequiresReview
            : ConfigurationUsageKeyResolution.NoVisibleConfigurationEntryFound;

        return new ConfigurationUsageFinding(
            kind,
            string.IsNullOrWhiteSpace(literalKey) ? null : literalKey,
            resolution,
            sourceFile.ProjectName,
            sourceFile.FullPath,
            lineNumber,
            TrimEvidence(node.ToString()),
            requiresReview);
    }

    private static IReadOnlyList<ConfiguredConfigurationKey> DiscoverConfiguredKeys(
        IEnumerable<ConfigurationInventoryFinding> findings)
    {
        return findings
            .SelectMany(CreateConfiguredKeys)
            .GroupBy(
                key => string.Join("|", key.Kind, key.Key, key.SourcePath),
                StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(key => key.Kind)
            .ThenBy(key => key.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(key => key.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static IEnumerable<ConfiguredConfigurationKey> CreateConfiguredKeys(
        ConfigurationInventoryFinding finding)
    {
        if (finding.Category == ConfigurationInventoryCategory.AppSetting &&
            finding.Confidence == ConfigurationInventoryConfidence.High &&
            !string.IsNullOrWhiteSpace(finding.Name))
        {
            yield return new ConfiguredConfigurationKey(
                ConfigurationUsageKind.AppSetting,
                finding.Name,
                finding.SourcePath);
        }

        if (finding.Category == ConfigurationInventoryCategory.ConnectionString &&
            finding.Confidence == ConfigurationInventoryConfidence.High &&
            !string.IsNullOrWhiteSpace(finding.Name))
        {
            yield return new ConfiguredConfigurationKey(
                ConfigurationUsageKind.ConnectionString,
                finding.Name,
                finding.SourcePath);
        }

        if (finding.Category == ConfigurationInventoryCategory.JsonConfiguration &&
            !string.IsNullOrWhiteSpace(finding.MaskedValue) &&
            !string.IsNullOrWhiteSpace(finding.Name))
        {
            yield return new ConfiguredConfigurationKey(
                ConfigurationUsageKind.AppSetting,
                finding.Name,
                finding.SourcePath);

            const string connectionStringPrefix = "ConnectionStrings:";

            if (finding.Name.StartsWith(connectionStringPrefix, StringComparison.OrdinalIgnoreCase))
            {
                yield return new ConfiguredConfigurationKey(
                    ConfigurationUsageKind.ConnectionString,
                    finding.Name[connectionStringPrefix.Length..],
                    finding.SourcePath);
            }
        }
    }

    private static IReadOnlyList<ConfigurationUsageFinding> ResolveSourceUsages(
        IEnumerable<ConfigurationUsageFinding> sourceUsages,
        IReadOnlyCollection<ConfiguredConfigurationKey> configuredKeys)
    {
        return sourceUsages
            .Select(usage => ResolveSourceUsage(usage, configuredKeys))
            .ToArray();
    }

    private static ConfigurationUsageFinding ResolveSourceUsage(
        ConfigurationUsageFinding usage,
        IEnumerable<ConfiguredConfigurationKey> configuredKeys)
    {
        if (string.IsNullOrWhiteSpace(usage.Key))
        {
            return usage with
            {
                Resolution = ConfigurationUsageKeyResolution.DynamicKeyRequiresReview,
                RequiresReview = true
            };
        }

        var hasMatch = configuredKeys.Any(key =>
            key.Kind == usage.Kind &&
            key.Key.Equals(usage.Key, StringComparison.OrdinalIgnoreCase));

        return usage with
        {
            Resolution = hasMatch
                ? ConfigurationUsageKeyResolution.MatchedVisibleConfigurationEntry
                : ConfigurationUsageKeyResolution.NoVisibleConfigurationEntryFound,
            RequiresReview = !hasMatch
        };
    }

    private static IReadOnlyList<ConfigurationKeyReconciliation> CreateKeyReconciliations(
        IEnumerable<ConfiguredConfigurationKey> configuredKeys,
        IReadOnlyCollection<ConfigurationUsageFinding> sourceUsages)
    {
        return configuredKeys
            .Select(key => CreateKeyReconciliation(key, sourceUsages))
            .OrderBy(reconciliation => reconciliation.Kind)
            .ThenBy(reconciliation => reconciliation.Key, StringComparer.OrdinalIgnoreCase)
            .ThenBy(reconciliation => reconciliation.ConfigSourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    private static ConfigurationKeyReconciliation CreateKeyReconciliation(
        ConfiguredConfigurationKey key,
        IEnumerable<ConfigurationUsageFinding> sourceUsages)
    {
        var hasStaticUsage = sourceUsages.Any(usage =>
            usage.Kind == key.Kind &&
            !string.IsNullOrWhiteSpace(usage.Key) &&
            usage.Key.Equals(key.Key, StringComparison.OrdinalIgnoreCase));

        return hasStaticUsage
            ? new ConfigurationKeyReconciliation(
                key.Kind,
                key.Key,
                key.SourcePath,
                ConfigurationStaticSourceUsage.Found,
                "Literal source usage matched.")
            : new ConfigurationKeyReconciliation(
                key.Kind,
                key.Key,
                key.SourcePath,
                ConfigurationStaticSourceUsage.NoStaticSourceUsageDetected,
                "This does not prove the key is unused. It may be used dynamically, by reflection, by config binding, by external tooling, or at runtime outside statically detected patterns.");
    }

    private static string CreateUsageDeduplicationKey(ConfigurationUsageFinding usage)
    {
        return string.Join(
            "|",
            usage.Kind,
            usage.Key ?? string.Empty,
            usage.ProjectName,
            usage.SourcePath,
            usage.LineNumber,
            usage.Evidence);
    }

    private static string TrimEvidence(string evidence)
    {
        return Regex.Replace(evidence, @"\s+", " ").Trim();
    }

    private static string? MaskSensitiveValue(string? value, string? key = null)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        var masked = value;

        masked = Regex.Replace(
            masked,
            @"(?i)(password|pwd|user\s*id|uid|accountkey|accesskey|sharedaccesskey|clientsecret|client_secret|secret|token|apikey|api_key|sig)\s*=\s*([^;&#""'\s]+)",
            match => $"{match.Groups[1].Value}=***");

        masked = Regex.Replace(
            masked,
            @"(?i)([?&](?:password|pwd|accountkey|accesskey|sharedaccesskey|clientsecret|client_secret|secret|token|apikey|api_key|sig|code)=)([^&#]*)",
            match => $"{match.Groups[1].Value}***");

        masked = Regex.Replace(
            masked,
            @"(?i)([a-z][a-z0-9+.-]*://)([^:/?#\s]+):([^@/?#\s]+)@",
            "$1***:***@");

        if (LooksSensitive(key) && string.Equals(masked, value, StringComparison.Ordinal))
        {
            return "***";
        }

        return masked;
    }

    private static bool LooksSensitive(string? key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        return key.Contains("password", StringComparison.OrdinalIgnoreCase)
               || key.Contains("pwd", StringComparison.OrdinalIgnoreCase)
               || key.Contains("secret", StringComparison.OrdinalIgnoreCase)
               || key.Contains("token", StringComparison.OrdinalIgnoreCase)
               || key.Contains("apikey", StringComparison.OrdinalIgnoreCase)
               || key.Contains("api_key", StringComparison.OrdinalIgnoreCase)
               || key.Contains("accesskey", StringComparison.OrdinalIgnoreCase)
               || key.Contains("clientsecret", StringComparison.OrdinalIgnoreCase)
               || key.Contains("client_secret", StringComparison.OrdinalIgnoreCase);
    }

    private static string GetFileNameOrFallback(string path, string fallback)
    {
        var fileName = Path.GetFileName(path);

        return string.IsNullOrWhiteSpace(fileName)
            ? fallback
            : fileName;
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
            ConfigurationInventoryCategory.ConfigurationApiUsage => 12,
            ConfigurationInventoryCategory.JsonConfiguration => 13,
            ConfigurationInventoryCategory.SettingsFile => 14,
            ConfigurationInventoryCategory.BuildPackageConfiguration => 15,
            _ => 100
        };
    }

    private static string CreateDeduplicationKey(ConfigurationInventoryFinding finding)
    {
        return string.Join(
            "|",
            finding.Category,
            finding.Name,
            finding.SourceType,
            finding.SourcePath,
            finding.ProjectName ?? string.Empty,
            finding.Evidence,
            finding.MaskedValue ?? string.Empty);
    }

    private sealed record JsonSetting(
        string Name,
        string? Value);

    private sealed record ConfigurationApiUsage(
        string Name,
        string Evidence,
        string MigrationConsideration);

    private sealed record ConfiguredConfigurationKey(
        ConfigurationUsageKind Kind,
        string Key,
        string SourcePath);
}




