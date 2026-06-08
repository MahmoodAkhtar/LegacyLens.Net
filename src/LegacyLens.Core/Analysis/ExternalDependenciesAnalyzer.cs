using System.Text.RegularExpressions;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Analysis;

public sealed class ExternalDependenciesAnalyzer
{
    private static readonly StringComparer Comparer = StringComparer.OrdinalIgnoreCase;

    public ExternalDependenciesReport Analyze(
        IReadOnlyCollection<DiscoveredProject> projects,
        IReadOnlyCollection<WcfEndpoint> wcfEndpoints,
        IReadOnlyCollection<DiscoveredConfigFile> configFiles)
    {
        ArgumentNullException.ThrowIfNull(projects);
        ArgumentNullException.ThrowIfNull(wcfEndpoints);
        ArgumentNullException.ThrowIfNull(configFiles);

        var dependencies = new List<ExternalDependency>();

        AddConfigFileDependencies(dependencies, configFiles);
        AddWcfEndpointDependencies(dependencies, wcfEndpoints);
        AddProjectPackageDependencies(dependencies, projects);
        AddAssemblyReferenceDependencies(dependencies, projects);

        var distinctDependencies = dependencies
            .GroupBy(CreateDeduplicationKey, StringComparer.OrdinalIgnoreCase)
            .Select(group => group.First())
            .OrderBy(dependency => GetCategoryPriority(dependency.Category))
            .ThenBy(dependency => dependency.Category.ToString(), StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.ProjectName ?? string.Empty, StringComparer.OrdinalIgnoreCase)
            .ThenBy(dependency => dependency.SourcePath, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return new ExternalDependenciesReport(distinctDependencies);
    }

    private static void AddConfigFileDependencies(
        ICollection<ExternalDependency> dependencies,
        IEnumerable<DiscoveredConfigFile> configFiles)
    {
        foreach (var configFile in configFiles)
        {
            AddConnectionStringDependencies(dependencies, configFile);
            AddAppSettingDependencies(dependencies, configFile);
            AddCustomSectionDependencies(dependencies, configFile);
        }
    }

    private static void AddConnectionStringDependencies(
        ICollection<ExternalDependency> dependencies,
        DiscoveredConfigFile configFile)
    {
        foreach (var connectionString in configFile.ConnectionStrings)
        {
            dependencies.Add(new ExternalDependency(
                ExternalDependencyCategory.Database,
                connectionString.Name,
                ExternalDependencySourceType.Configuration,
                configFile.FilePath,
                ProjectName: null,
                Evidence: CreateConnectionStringEvidence(connectionString),
                MaskedValue: connectionString.MaskedConnectionString,
                ExternalDependencyConfidence.High,
                RequiresConfirmation: true,
                Notes: "Connection strings usually indicate possible database or external data dependencies. Runtime usage is not verified."));
        }

        if (configFile.ConnectionStrings.Count == 0 && configFile.ConnectionStringsCount > 0)
        {
            dependencies.Add(new ExternalDependency(
                ExternalDependencyCategory.Database,
                GetFileNameOrFallback(configFile.FilePath, "Connection strings"),
                ExternalDependencySourceType.Configuration,
                configFile.FilePath,
                ProjectName: null,
                Evidence: $"{configFile.ConnectionStringsCount} connection string(s) configured.",
                MaskedValue: null,
                ExternalDependencyConfidence.Medium,
                RequiresConfirmation: true,
                Notes: "Connection string count indicates possible database or external data dependencies. Connection string details were not available."));
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

    private static void AddAppSettingDependencies(
        ICollection<ExternalDependency> dependencies,
        DiscoveredConfigFile configFile)
    {
        foreach (var appSetting in configFile.AppSettings)
        {
            var signal = ClassifyAppSetting(appSetting.Key, appSetting.MaskedValue);

            if (signal is null)
            {
                continue;
            }

            dependencies.Add(new ExternalDependency(
                signal.Value.Category,
                appSetting.Key,
                ExternalDependencySourceType.Configuration,
                configFile.FilePath,
                ProjectName: null,
                Evidence: signal.Value.Evidence,
                MaskedValue: appSetting.MaskedValue,
                signal.Value.Confidence,
                RequiresConfirmation: true,
                Notes: signal.Value.Notes));
        }
    }

    private static void AddCustomSectionDependencies(
        ICollection<ExternalDependency> dependencies,
        DiscoveredConfigFile configFile)
    {
        foreach (var customSection in configFile.CustomSections)
        {
            var signal = ClassifyCustomSection(customSection.Name, customSection.Type);

            if (signal is null)
            {
                continue;
            }

            dependencies.Add(new ExternalDependency(
                signal.Value.Category,
                customSection.Name,
                ExternalDependencySourceType.Configuration,
                configFile.FilePath,
                ProjectName: null,
                Evidence: signal.Value.Evidence,
                MaskedValue: null,
                signal.Value.Confidence,
                RequiresConfirmation: true,
                Notes: signal.Value.Notes));
        }
    }

    private static void AddWcfEndpointDependencies(
        ICollection<ExternalDependency> dependencies,
        IEnumerable<WcfEndpoint> wcfEndpoints)
    {
        foreach (var endpoint in wcfEndpoints)
        {
            var name = FirstNonWhiteSpace(
                endpoint.ServiceName,
                endpoint.Contract,
                endpoint.Address,
                "Configured WCF endpoint");

            var evidenceParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(endpoint.Binding))
            {
                evidenceParts.Add($"{endpoint.Binding} endpoint configured");
            }
            else
            {
                evidenceParts.Add("WCF endpoint configured");
            }

            if (!string.IsNullOrWhiteSpace(endpoint.Contract))
            {
                evidenceParts.Add($"contract {endpoint.Contract}");
            }

            if (!string.IsNullOrWhiteSpace(endpoint.BindingConfiguration))
            {
                evidenceParts.Add($"binding configuration {endpoint.BindingConfiguration}");
            }

            if (!string.IsNullOrWhiteSpace(endpoint.BehaviorConfiguration))
            {
                evidenceParts.Add($"behaviour configuration {endpoint.BehaviorConfiguration}");
            }

            if (!string.IsNullOrWhiteSpace(endpoint.Address))
            {
                evidenceParts.Add($"address {MaskSensitiveValue(endpoint.Address)}");
            }

            if (endpoint.IsMetadataExchangeEndpoint)
            {
                evidenceParts.Add("metadata exchange endpoint");
            }

            dependencies.Add(new ExternalDependency(
                ExternalDependencyCategory.WcfServiceEndpoint,
                name,
                ExternalDependencySourceType.WcfEndpoint,
                endpoint.ConfigFilePath,
                ProjectName: null,
                Evidence: string.Join("; ", evidenceParts) + ".",
                MaskedValue: MaskSensitiveValue(endpoint.Address),
                ExternalDependencyConfidence.High,
                RequiresConfirmation: true,
                Notes: "Configured WCF endpoints may represent runtime service boundaries or integration points. Runtime usage is not verified."));
        }
    }

    private static void AddProjectPackageDependencies(
        ICollection<ExternalDependency> dependencies,
        IEnumerable<DiscoveredProject> projects)
    {
        foreach (var project in projects)
        {
            AddDetailedPackageDependencies(dependencies, project);
            AddPackageNameDependencies(dependencies, project);
        }
    }

    private static void AddDetailedPackageDependencies(
        ICollection<ExternalDependency> dependencies,
        DiscoveredProject project)
    {
        foreach (var packageReference in project.PackageReferenceDetails)
        {
            if (string.IsNullOrWhiteSpace(packageReference.Name))
            {
                continue;
            }

            var packageSignal = ClassifyPackage(packageReference.Name);

            if (packageSignal is null)
            {
                continue;
            }

            var versionText = string.IsNullOrWhiteSpace(packageReference.Version)
                ? "unknown version"
                : packageReference.Version;

            var sourcePath = FirstNonWhiteSpace(
                packageReference.SourcePath,
                project.ProjectFilePath);

            dependencies.Add(new ExternalDependency(
                packageSignal.Value.Category,
                packageReference.Name,
                ExternalDependencySourceType.PackageReference,
                sourcePath,
                project.Name,
                Evidence: $"{packageReference.Name} {versionText} package reference found.",
                MaskedValue: null,
                packageSignal.Value.Confidence,
                RequiresConfirmation: true,
                Notes: packageSignal.Value.Notes));
        }
    }

    private static void AddPackageNameDependencies(
        ICollection<ExternalDependency> dependencies,
        DiscoveredProject project)
    {
        var detailedPackageNames = project.PackageReferenceDetails
            .Select(package => package.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var packageName in project.PackageReferences)
        {
            if (string.IsNullOrWhiteSpace(packageName))
            {
                continue;
            }

            if (detailedPackageNames.Contains(packageName))
            {
                continue;
            }

            var packageSignal = ClassifyPackage(packageName);

            if (packageSignal is null)
            {
                continue;
            }

            dependencies.Add(new ExternalDependency(
                packageSignal.Value.Category,
                packageName,
                ExternalDependencySourceType.PackageReference,
                project.ProjectFilePath,
                project.Name,
                Evidence: $"{packageName} package reference found.",
                MaskedValue: null,
                packageSignal.Value.Confidence,
                RequiresConfirmation: true,
                Notes: packageSignal.Value.Notes));
        }
    }

    private static void AddAssemblyReferenceDependencies(
        ICollection<ExternalDependency> dependencies,
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

                var assemblySignal = ClassifyAssemblyReference(assemblyReference);

                if (assemblySignal is null)
                {
                    continue;
                }

                dependencies.Add(new ExternalDependency(
                    assemblySignal.Value.Category,
                    assemblyReference,
                    ExternalDependencySourceType.AssemblyReference,
                    project.ProjectFilePath,
                    project.Name,
                    Evidence: $"{assemblyReference} assembly reference found.",
                    MaskedValue: null,
                    assemblySignal.Value.Confidence,
                    RequiresConfirmation: true,
                    Notes: assemblySignal.Value.Notes));
            }
        }
    }

    private static AppSettingSignal? ClassifyAppSetting(string key, string? value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return null;
        }

        var valueOrEmpty = value ?? string.Empty;

        if (LooksLikeUrl(valueOrEmpty) || KeyLooksLikeHttpEndpoint(key))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.HttpApi,
                ExternalDependencyConfidence.Medium,
                "HTTP/API endpoint setting found.",
                "App setting key or value may indicate an HTTP API or service endpoint dependency.");
        }

        if (KeyContainsAny(key, "queue", "topic", "subscription", "servicebus", "service bus"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.MessagingQueue,
                ExternalDependencyConfidence.Medium,
                "Messaging-related app setting found.",
                "App setting may indicate a queue, topic, subscription, broker, or service bus dependency.");
        }

        if (LooksLikeUncPath(valueOrEmpty) || LooksLikeWindowsAbsolutePath(valueOrEmpty) || KeyContainsAny(key, "path", "folder", "directory", "share"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.FileSystemFileShare,
                ExternalDependencyConfidence.Medium,
                "File system or file share setting found.",
                "App setting may indicate a local path, network share, import folder, export folder, or file system dependency.");
        }

        if (KeyContainsAny(key, "smtp", "mail", "email", "sendgrid"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.EmailSmtp,
                ExternalDependencyConfidence.Medium,
                "Email/SMTP-related app setting found.",
                "App setting may indicate SMTP, email delivery, or third-party email service dependency.");
        }

        if (KeyContainsAny(key, "redis", "cache", "distributedcache", "distributed cache"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.CacheDistributedState,
                ExternalDependencyConfidence.Medium,
                "Cache-related app setting found.",
                "App setting may indicate Redis, distributed cache, or shared state dependency.");
        }

        if (KeyContainsAny(key, "authority", "issuer", "audience", "tenant", "clientid", "client id", "ida:clientid", "ida:tenant"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.AuthenticationIdentityProvider,
                ExternalDependencyConfidence.Medium,
                "Identity provider setting found.",
                "App setting may indicate an external identity provider, token issuer, authority, tenant, audience, or client registration.");
        }

        if (KeyContainsAny(key, "storage", "blob", "azure", "aws", "s3", "applicationinsights", "appinsights", "instrumentationkey"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.CloudService,
                ExternalDependencyConfidence.Medium,
                "Cloud service setting found.",
                "App setting may indicate cloud storage, telemetry, or platform service dependency.");
        }

        return null;
    }

    private static AppSettingSignal? ClassifyCustomSection(string name, string? type)
    {
        var combined = string.Join(" ", name, type ?? string.Empty);

        if (KeyContainsAny(combined, "smtp", "mail", "email"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.EmailSmtp,
                ExternalDependencyConfidence.Low,
                "Email-related custom configuration section found.",
                "Custom configuration section may contain SMTP or email service configuration. Values and runtime usage require confirmation.");
        }

        if (KeyContainsAny(combined, "redis", "cache"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.CacheDistributedState,
                ExternalDependencyConfidence.Low,
                "Cache-related custom configuration section found.",
                "Custom configuration section may contain cache or distributed state configuration. Values and runtime usage require confirmation.");
        }

        if (KeyContainsAny(combined, "service", "endpoint", "client", "api"))
        {
            return new AppSettingSignal(
                ExternalDependencyCategory.HttpApi,
                ExternalDependencyConfidence.Low,
                "Service-related custom configuration section found.",
                "Custom configuration section may contain service endpoint or client configuration. Values and runtime usage require confirmation.");
        }

        return null;
    }

    private static DependencySignal? ClassifyPackage(string packageName)
    {
        if (MatchesAny(
                packageName,
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "Npgsql",
                "MySql.Data",
                "MySqlConnector",
                "Oracle.ManagedDataAccess"))
        {
            return new DependencySignal(
                ExternalDependencyCategory.Database,
                ExternalDependencyConfidence.Medium,
                "Database client package may indicate a database dependency. Connection details and runtime usage require confirmation.");
        }

        if (packageName.StartsWith("System.ServiceModel.", StringComparison.OrdinalIgnoreCase))
        {
            return new DependencySignal(
                ExternalDependencyCategory.WcfServiceEndpoint,
                ExternalDependencyConfidence.High,
                "WCF-related package may indicate service endpoint dependencies or WCF client/server usage.");
        }

        if (MatchesAny(
                packageName,
                "RabbitMQ.Client",
                "MassTransit",
                "NServiceBus",
                "Microsoft.Azure.ServiceBus",
                "Azure.Messaging.ServiceBus"))
        {
            return new DependencySignal(
                ExternalDependencyCategory.MessagingQueue,
                ExternalDependencyConfidence.Medium,
                "Messaging package may indicate a broker, queue, topic, subscription, or service bus dependency.");
        }

        if (MatchesAny(
                packageName,
                "StackExchange.Redis",
                "Microsoft.Extensions.Caching.StackExchangeRedis"))
        {
            return new DependencySignal(
                ExternalDependencyCategory.CacheDistributedState,
                ExternalDependencyConfidence.Medium,
                "Redis/cache package may indicate distributed cache or shared state dependency.");
        }

        if (MatchesAny(
                packageName,
                "SendGrid",
                "MailKit",
                "Microsoft.Azure.WebJobs.Extensions.SendGrid"))
        {
            return new DependencySignal(
                ExternalDependencyCategory.EmailSmtp,
                ExternalDependencyConfidence.Medium,
                "Email package may indicate SMTP or third-party email delivery dependency.");
        }

        if (IsIdentityPackage(packageName))
        {
            return new DependencySignal(
                ExternalDependencyCategory.AuthenticationIdentityProvider,
                ExternalDependencyConfidence.Medium,
                "Identity or authentication package may indicate dependency on an external identity provider or token issuer.");
        }

        if (IsCloudPackage(packageName))
        {
            return new DependencySignal(
                ExternalDependencyCategory.CloudService,
                ExternalDependencyConfidence.Medium,
                "Cloud service package may indicate dependency on cloud infrastructure, telemetry, storage, messaging, or platform services.");
        }

        return null;
    }

    private static DependencySignal? ClassifyAssemblyReference(string assemblyReference)
    {
        var assemblyName = StripAssemblyMetadata(assemblyReference);

        if (MatchesAny(
                assemblyName,
                "System.Data.SqlClient",
                "Microsoft.Data.SqlClient",
                "Npgsql",
                "MySql.Data",
                "MySqlConnector",
                "Oracle.ManagedDataAccess"))
        {
            return new DependencySignal(
                ExternalDependencyCategory.Database,
                ExternalDependencyConfidence.Medium,
                "Database assembly reference may indicate a database dependency. Runtime usage requires confirmation.");
        }

        if (MatchesAny(assemblyName, "System.ServiceModel"))
        {
            return new DependencySignal(
                ExternalDependencyCategory.WcfServiceEndpoint,
                ExternalDependencyConfidence.High,
                "System.ServiceModel assembly reference may indicate WCF service or client usage.");
        }

        if (MatchesAny(assemblyName, "System.Net.Mail"))
        {
            return new DependencySignal(
                ExternalDependencyCategory.EmailSmtp,
                ExternalDependencyConfidence.Low,
                "Mail assembly reference may indicate SMTP or email delivery usage. Runtime usage requires confirmation.");
        }

        if (LooksLikeVendorAssembly(assemblyName))
        {
            return new DependencySignal(
                ExternalDependencyCategory.ExternalAssemblyVendorDll,
                ExternalDependencyConfidence.Low,
                "Non-framework assembly reference may indicate a vendor, local, or external binary dependency. Confirm whether the DLL is required at build time or runtime.");
        }

        return null;
    }

    private static bool IsCloudPackage(string packageName)
    {
        return packageName.StartsWith("Azure.", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("Microsoft.Azure.", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("WindowsAzure.", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("AWSSDK.", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("Google.Cloud.", StringComparison.OrdinalIgnoreCase)
            || MatchesAny(
                packageName,
                "WindowsAzure.Storage",
                "Microsoft.ApplicationInsights",
                "Microsoft.Extensions.Logging.ApplicationInsights");
    }

    private static bool IsIdentityPackage(string packageName)
    {
        return packageName.StartsWith("Microsoft.AspNet.Identity.", StringComparison.OrdinalIgnoreCase)
            || packageName.StartsWith("Microsoft.IdentityModel.", StringComparison.OrdinalIgnoreCase)
            || MatchesAny(
                packageName,
                "Microsoft.Identity.Web",
                "Microsoft.Owin.Security.OpenIdConnect",
                "System.IdentityModel.Tokens.Jwt",
                "Azure.Identity");
    }

    private static bool LooksLikeVendorAssembly(string assemblyName)
    {
        if (string.IsNullOrWhiteSpace(assemblyName))
        {
            return false;
        }

        if (assemblyName.Equals("System", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("System.", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("Microsoft", StringComparison.OrdinalIgnoreCase)
            || assemblyName.StartsWith("Microsoft.", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("mscorlib", StringComparison.OrdinalIgnoreCase)
            || assemblyName.Equals("netstandard", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
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

    private static bool KeyLooksLikeHttpEndpoint(string key)
    {
        return key.EndsWith("Url", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("Uri", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("Endpoint", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("BaseAddress", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("BaseUrl", StringComparison.OrdinalIgnoreCase)
            || key.EndsWith("ApiUrl", StringComparison.OrdinalIgnoreCase)
            || KeyContainsAny(key, "url", "uri", "endpoint", "baseaddress", "base address", "baseurl", "base url", "apiurl", "api url");
    }

    private static bool LooksLikeUrl(string value)
    {
        return value.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || value.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
    }

    private static bool LooksLikeUncPath(string value)
    {
        return value.StartsWith(@"\\", StringComparison.Ordinal);
    }

    private static bool LooksLikeWindowsAbsolutePath(string value)
    {
        return Regex.IsMatch(value, @"^[A-Za-z]:\\");
    }

    private static bool KeyContainsAny(string value, params string[] parts)
    {
        return parts.Any(part => value.Contains(part, StringComparison.OrdinalIgnoreCase));
    }

    private static string MaskSensitiveValue(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return string.Empty;
        }

        var masked = value;

        masked = Regex.Replace(
            masked,
            @"(?i)(password|pwd|user\s*id|uid|accountkey|accesskey|sharedaccesskey|clientsecret|client_secret|secret|token|apikey|api_key|sig)\s*=\s*([^;""'\s]+)",
            match => $"{match.Groups[1].Value}=***");

        masked = Regex.Replace(
            masked,
            @"(?i)([?&](password|pwd|accountkey|accesskey|sharedaccesskey|clientsecret|client_secret|secret|token|apikey|api_key|sig|code)=)([^&#]+)",
            "$1***");

        masked = Regex.Replace(
            masked,
            @"(?i)(https?://)([^:/?#\s]+):([^@/?#\s]+)@",
            "$1***:***@");

        return masked;
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

    private static string CreateDeduplicationKey(ExternalDependency dependency)
    {
        return string.Join(
            "|",
            dependency.Category,
            dependency.Name,
            dependency.SourceType,
            dependency.SourcePath,
            dependency.ProjectName ?? string.Empty,
            dependency.Evidence);
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

    private readonly record struct DependencySignal(
        ExternalDependencyCategory Category,
        ExternalDependencyConfidence Confidence,
        string Notes);

    private readonly record struct AppSettingSignal(
        ExternalDependencyCategory Category,
        ExternalDependencyConfidence Confidence,
        string Evidence,
        string Notes);
}