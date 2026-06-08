using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ExternalDependenciesAnalyzerTests
{
    [Fact]
    public void Analyze_WhenProjectsIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ExternalDependenciesAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                null!,
                Array.Empty<WcfEndpoint>(),
                Array.Empty<DiscoveredConfigFile>()));

        Assert.Equal("projects", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenWcfEndpointsIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ExternalDependenciesAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                null!,
                Array.Empty<DiscoveredConfigFile>()));

        Assert.Equal("wcfEndpoints", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenConfigFilesIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ExternalDependenciesAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                null!));

        Assert.Equal("configFiles", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenNoEvidenceExists_ReturnsEmptyReport()
    {
        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Empty(report.Dependencies);
    }

    [Fact]
    public void Analyze_WhenConnectionStringExists_AddsDatabaseDependency()
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

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.Database, dependency.Category);
        Assert.Equal("MainDatabase", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.Configuration, dependency.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", dependency.SourcePath);
        Assert.Null(dependency.ProjectName);
        Assert.Equal("Connection string configured with provider System.Data.SqlClient.", dependency.Evidence);
        Assert.Equal("Server=.;Database=Main;User Id=***;Password=***;", dependency.MaskedValue);
        Assert.Equal(ExternalDependencyConfidence.High, dependency.Confidence);
        Assert.True(dependency.RequiresConfirmation);
        Assert.Contains("Runtime usage is not verified", dependency.Notes);
    }

    [Fact]
    public void Analyze_WhenOnlyConnectionStringCountExists_AddsDatabaseDependencyFromCount()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                ConnectionStringsCount = 2
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.Database, dependency.Category);
        Assert.Equal("Web.config", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.Configuration, dependency.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", dependency.SourcePath);
        Assert.Equal("2 connection string(s) configured.", dependency.Evidence);
        Assert.Null(dependency.MaskedValue);
        Assert.Equal(ExternalDependencyConfidence.Medium, dependency.Confidence);
        Assert.True(dependency.RequiresConfirmation);
    }

    [Fact]
    public void Analyze_WhenAppSettingValueLooksLikeUrl_AddsHttpApiDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "PaymentApiBaseUrl",
                        MaskedValue = "https://payments.example.test/api"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.HttpApi, dependency.Category);
        Assert.Equal("PaymentApiBaseUrl", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.Configuration, dependency.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", dependency.SourcePath);
        Assert.Equal("HTTP/API endpoint setting found.", dependency.Evidence);
        Assert.Equal("https://payments.example.test/api", dependency.MaskedValue);
        Assert.Equal(ExternalDependencyConfidence.Medium, dependency.Confidence);
        Assert.True(dependency.RequiresConfirmation);
    }

    [Fact]
    public void Analyze_WhenAppSettingKeyLooksLikeEndpoint_AddsHttpApiDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\App.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "CustomerEndpoint",
                        MaskedValue = "customer-service"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.HttpApi, dependency.Category);
        Assert.Equal("CustomerEndpoint", dependency.Name);
        Assert.Equal("customer-service", dependency.MaskedValue);
    }

    [Fact]
    public void Analyze_WhenAppSettingKeyLooksLikeQueue_AddsMessagingDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\App.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "OrderQueueName",
                        MaskedValue = "orders"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.MessagingQueue, dependency.Category);
        Assert.Equal("OrderQueueName", dependency.Name);
        Assert.Equal("Messaging-related app setting found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenAppSettingValueLooksLikeUncPath_AddsFileSystemDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\App.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ExportShare",
                        MaskedValue = @"\\fileserver\exports"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.FileSystemFileShare, dependency.Category);
        Assert.Equal("ExportShare", dependency.Name);
        Assert.Equal(@"\\fileserver\exports", dependency.MaskedValue);
        Assert.Equal("File system or file share setting found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenAppSettingValueLooksLikeWindowsAbsolutePath_AddsFileSystemDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\App.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ImportFolder",
                        MaskedValue = @"C:\Imports"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.FileSystemFileShare, dependency.Category);
        Assert.Equal("ImportFolder", dependency.Name);
        Assert.Equal(@"C:\Imports", dependency.MaskedValue);
    }

    [Fact]
    public void Analyze_WhenAppSettingKeyLooksLikeSmtp_AddsEmailDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "SmtpServer",
                        MaskedValue = "smtp.example.test"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.EmailSmtp, dependency.Category);
        Assert.Equal("SmtpServer", dependency.Name);
        Assert.Equal("Email/SMTP-related app setting found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenAppSettingKeyLooksLikeRedis_AddsCacheDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "RedisConnection",
                        MaskedValue = "redis.example.test:6379"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.CacheDistributedState, dependency.Category);
        Assert.Equal("RedisConnection", dependency.Name);
        Assert.Equal("Cache-related app setting found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenAppSettingKeyLooksLikeIdentityProvider_AddsIdentityDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ida:Tenant",
                        MaskedValue = "tenant-id"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.AuthenticationIdentityProvider, dependency.Category);
        Assert.Equal("ida:Tenant", dependency.Name);
        Assert.Equal("Identity provider setting found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenAppSettingKeyLooksLikeCloudService_AddsCloudDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 1,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "AzureStorageContainer",
                        MaskedValue = "exports"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.CloudService, dependency.Category);
        Assert.Equal("AzureStorageContainer", dependency.Name);
        Assert.Equal("Cloud service setting found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenCustomSectionLooksLikeServiceConfiguration_AddsHttpApiDependency()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                CustomSectionCount = 1,
                CustomSections =
                {
                    new DiscoveredConfigSection
                    {
                        Name = "serviceClients",
                        Type = "Legacy.ServiceClientsSection, Legacy"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.HttpApi, dependency.Category);
        Assert.Equal("serviceClients", dependency.Name);
        Assert.Equal(ExternalDependencyConfidence.Low, dependency.Confidence);
        Assert.Equal("Service-related custom configuration section found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenWcfEndpointExists_AddsWcfDependency()
    {
        var wcfEndpoints = new[]
        {
            new WcfEndpoint
            {
                ConfigFilePath = @"C:\Repo\Web.config",
                ServiceName = "Legacy.CustomerService",
                Address = "https://services.example.test/customer?token=***",
                Binding = "basicHttpBinding",
                Contract = "Legacy.ICustomerService",
                BindingConfiguration = "CustomerBinding",
                BehaviorConfiguration = "CustomerBehaviour",
                IsMetadataExchangeEndpoint = false
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            wcfEndpoints,
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.WcfServiceEndpoint, dependency.Category);
        Assert.Equal("Legacy.CustomerService", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.WcfEndpoint, dependency.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", dependency.SourcePath);
        Assert.Null(dependency.ProjectName);
        Assert.Contains("basicHttpBinding endpoint configured", dependency.Evidence);
        Assert.Contains("contract Legacy.ICustomerService", dependency.Evidence);
        Assert.Contains("binding configuration CustomerBinding", dependency.Evidence);
        Assert.Contains("behaviour configuration CustomerBehaviour", dependency.Evidence);
        Assert.Contains("address https://services.example.test/customer?token=***", dependency.Evidence);
        Assert.Equal("https://services.example.test/customer?token=***", dependency.MaskedValue);
        Assert.Equal(ExternalDependencyConfidence.High, dependency.Confidence);
        Assert.True(dependency.RequiresConfirmation);
    }

    [Fact]
    public void Analyze_WhenWcfEndpointIsMetadataExchangeEndpoint_IncludesMetadataEvidence()
    {
        var wcfEndpoints = new[]
        {
            new WcfEndpoint
            {
                ConfigFilePath = @"C:\Repo\Web.config",
                ServiceName = "Legacy.CustomerService",
                Address = "mex",
                Binding = "mexHttpBinding",
                Contract = "IMetadataExchange",
                IsMetadataExchangeEndpoint = true
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            wcfEndpoints,
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.WcfServiceEndpoint, dependency.Category);
        Assert.Contains("metadata exchange endpoint", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenDatabasePackageExists_AddsDatabaseDependency()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Data",
                ProjectFilePath = @"C:\Repo\Legacy.Data\Legacy.Data.csproj",
                PackageReferenceDetails =
                {
                    new DiscoveredPackageReference
                    {
                        Name = "Microsoft.Data.SqlClient",
                        Version = "5.2.0",
                        SourceFormat = "PackageReference",
                        SourcePath = @"C:\Repo\Legacy.Data\Legacy.Data.csproj"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.Database, dependency.Category);
        Assert.Equal("Microsoft.Data.SqlClient", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.PackageReference, dependency.SourceType);
        Assert.Equal(@"C:\Repo\Legacy.Data\Legacy.Data.csproj", dependency.SourcePath);
        Assert.Equal("Legacy.Data", dependency.ProjectName);
        Assert.Equal("Microsoft.Data.SqlClient 5.2.0 package reference found.", dependency.Evidence);
        Assert.Equal(ExternalDependencyConfidence.Medium, dependency.Confidence);
    }

    [Fact]
    public void Analyze_WhenDetailedPackageVersionIsMissing_UsesUnknownVersionInEvidence()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Services",
                ProjectFilePath = @"C:\Repo\Legacy.Services\Legacy.Services.csproj",
                PackageReferenceDetails =
                {
                    new DiscoveredPackageReference
                    {
                        Name = "System.ServiceModel.Http",
                        Version = null,
                        SourceFormat = "PackageReference",
                        SourcePath = @"C:\Repo\Legacy.Services\Legacy.Services.csproj"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.WcfServiceEndpoint, dependency.Category);
        Assert.Equal("System.ServiceModel.Http unknown version package reference found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenOnlyPackageNameExists_AddsPackageDependencyFromProjectFile()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Messaging",
                ProjectFilePath = @"C:\Repo\Legacy.Messaging\Legacy.Messaging.csproj",
                PackageReferences =
                {
                    "RabbitMQ.Client"
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.MessagingQueue, dependency.Category);
        Assert.Equal("RabbitMQ.Client", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.PackageReference, dependency.SourceType);
        Assert.Equal(@"C:\Repo\Legacy.Messaging\Legacy.Messaging.csproj", dependency.SourcePath);
        Assert.Equal("Legacy.Messaging", dependency.ProjectName);
        Assert.Equal("RabbitMQ.Client package reference found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenPackageExistsInDetailsAndNameList_DoesNotDuplicatePackageDependency()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Messaging",
                ProjectFilePath = @"C:\Repo\Legacy.Messaging\Legacy.Messaging.csproj",
                PackageReferences =
                {
                    "RabbitMQ.Client"
                },
                PackageReferenceDetails =
                {
                    new DiscoveredPackageReference
                    {
                        Name = "RabbitMQ.Client",
                        Version = "7.0.0",
                        SourceFormat = "PackageReference",
                        SourcePath = @"C:\Repo\Legacy.Messaging\Legacy.Messaging.csproj"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal("RabbitMQ.Client", dependency.Name);
        Assert.Equal("RabbitMQ.Client 7.0.0 package reference found.", dependency.Evidence);
    }

    [Theory]
    [InlineData("RabbitMQ.Client", ExternalDependencyCategory.MessagingQueue)]
    [InlineData("MassTransit", ExternalDependencyCategory.MessagingQueue)]
    [InlineData("NServiceBus", ExternalDependencyCategory.MessagingQueue)]
    [InlineData("Microsoft.Azure.ServiceBus", ExternalDependencyCategory.MessagingQueue)]
    [InlineData("Azure.Messaging.ServiceBus", ExternalDependencyCategory.MessagingQueue)]
    [InlineData("StackExchange.Redis", ExternalDependencyCategory.CacheDistributedState)]
    [InlineData("Microsoft.Extensions.Caching.StackExchangeRedis", ExternalDependencyCategory.CacheDistributedState)]
    [InlineData("SendGrid", ExternalDependencyCategory.EmailSmtp)]
    [InlineData("MailKit", ExternalDependencyCategory.EmailSmtp)]
    [InlineData("Azure.Storage.Blobs", ExternalDependencyCategory.CloudService)]
    [InlineData("Microsoft.Azure.WebJobs", ExternalDependencyCategory.CloudService)]
    [InlineData("WindowsAzure.Storage", ExternalDependencyCategory.CloudService)]
    [InlineData("AWSSDK.S3", ExternalDependencyCategory.CloudService)]
    [InlineData("Google.Cloud.Storage.V1", ExternalDependencyCategory.CloudService)]
    [InlineData("Microsoft.ApplicationInsights", ExternalDependencyCategory.CloudService)]
    [InlineData("Microsoft.Identity.Web", ExternalDependencyCategory.AuthenticationIdentityProvider)]
    [InlineData("Microsoft.Owin.Security.OpenIdConnect", ExternalDependencyCategory.AuthenticationIdentityProvider)]
    [InlineData("System.IdentityModel.Tokens.Jwt", ExternalDependencyCategory.AuthenticationIdentityProvider)]
    [InlineData("Azure.Identity", ExternalDependencyCategory.AuthenticationIdentityProvider)]
    public void Analyze_WhenKnownInfrastructurePackageExists_AddsExpectedDependencyCategory(
        string packageName,
        ExternalDependencyCategory expectedCategory)
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.App",
                ProjectFilePath = @"C:\Repo\Legacy.App\Legacy.App.csproj",
                PackageReferenceDetails =
                {
                    new DiscoveredPackageReference
                    {
                        Name = packageName,
                        Version = "1.0.0",
                        SourceFormat = "PackageReference",
                        SourcePath = @"C:\Repo\Legacy.App\Legacy.App.csproj"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(expectedCategory, dependency.Category);
        Assert.Equal(packageName, dependency.Name);
    }

    [Fact]
    public void Analyze_WhenSystemServiceModelAssemblyReferenceExists_AddsWcfDependency()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Services",
                ProjectFilePath = @"C:\Repo\Legacy.Services\Legacy.Services.csproj",
                AssemblyReferences =
                {
                    "System.ServiceModel"
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.WcfServiceEndpoint, dependency.Category);
        Assert.Equal("System.ServiceModel", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.AssemblyReference, dependency.SourceType);
        Assert.Equal(@"C:\Repo\Legacy.Services\Legacy.Services.csproj", dependency.SourcePath);
        Assert.Equal("Legacy.Services", dependency.ProjectName);
        Assert.Equal("System.ServiceModel assembly reference found.", dependency.Evidence);
        Assert.Equal(ExternalDependencyConfidence.High, dependency.Confidence);
    }

    [Fact]
    public void Analyze_WhenDatabaseAssemblyReferenceExistsWithVersionMetadata_StripsMetadataForClassification()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Data",
                ProjectFilePath = @"C:\Repo\Legacy.Data\Legacy.Data.csproj",
                AssemblyReferences =
                {
                    "System.Data.SqlClient, Version=4.6.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a"
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.Database, dependency.Category);
        Assert.Equal("System.Data.SqlClient, Version=4.6.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a", dependency.Name);
        Assert.Equal("System.Data.SqlClient, Version=4.6.1.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a assembly reference found.", dependency.Evidence);
    }

    [Fact]
    public void Analyze_WhenVendorAssemblyReferenceExists_AddsExternalAssemblyDependency()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Web",
                ProjectFilePath = @"C:\Repo\Legacy.Web\Legacy.Web.csproj",
                AssemblyReferences =
                {
                    "Contoso.Legacy.PaymentGateway"
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal(ExternalDependencyCategory.ExternalAssemblyVendorDll, dependency.Category);
        Assert.Equal("Contoso.Legacy.PaymentGateway", dependency.Name);
        Assert.Equal(ExternalDependencySourceType.AssemblyReference, dependency.SourceType);
        Assert.Equal("Legacy.Web", dependency.ProjectName);
        Assert.Equal(ExternalDependencyConfidence.Low, dependency.Confidence);
    }

    [Theory]
    [InlineData("System")]
    [InlineData("System.Web")]
    [InlineData("Microsoft.CSharp")]
    [InlineData("mscorlib")]
    [InlineData("netstandard")]
    public void Analyze_WhenFrameworkAssemblyReferenceExists_DoesNotAddVendorAssemblyDependency(string assemblyReference)
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.Web",
                ProjectFilePath = @"C:\Repo\Legacy.Web\Legacy.Web.csproj",
                AssemblyReferences =
                {
                    assemblyReference
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Empty(report.Dependencies);
    }

    [Fact]
    public void Analyze_WhenSameDependencyIsFoundTwice_DeDuplicatesEquivalentFindings()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 2,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "PaymentApiBaseUrl",
                        MaskedValue = "https://payments.example.test"
                    },
                    new DiscoveredAppSetting
                    {
                        Key = "PaymentApiBaseUrl",
                        MaskedValue = "https://payments.example.test"
                    }
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            configFiles);

        var dependency = Assert.Single(report.Dependencies);

        Assert.Equal("PaymentApiBaseUrl", dependency.Name);
        Assert.Equal(ExternalDependencyCategory.HttpApi, dependency.Category);
    }

    [Fact]
    public void Analyze_ReturnsDependenciesInCategoryPriorityOrder()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "Legacy.App",
                ProjectFilePath = @"C:\Repo\Legacy.App\Legacy.App.csproj",
                PackageReferences =
                {
                    "StackExchange.Redis",
                    "RabbitMQ.Client",
                    "Microsoft.Data.SqlClient"
                }
            }
        };

        var analyzer = new ExternalDependenciesAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Collection(
            report.Dependencies,
            dependency => Assert.Equal(ExternalDependencyCategory.Database, dependency.Category),
            dependency => Assert.Equal(ExternalDependencyCategory.MessagingQueue, dependency.Category),
            dependency => Assert.Equal(ExternalDependencyCategory.CacheDistributedState, dependency.Category));
    }
}