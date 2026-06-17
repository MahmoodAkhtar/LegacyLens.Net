using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Files;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ConfigurationInventoryAnalyzerTests : IDisposable
{
    private readonly string _rootPath;
    private readonly string _projectDirectory;
    private readonly string _projectFilePath;

    public ConfigurationInventoryAnalyzerTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ConfigurationInventoryAnalyzerTests",
            Guid.NewGuid().ToString("N"));

        _projectDirectory = Path.Combine(_rootPath, "SampleLegacyApp.Web");
        Directory.CreateDirectory(_projectDirectory);

        _projectFilePath = Path.Combine(_projectDirectory, "SampleLegacyApp.Web.csproj");

        File.WriteAllText(
            _projectFilePath,
            """
            <Project Sdk="Microsoft.NET.Sdk">
              <PropertyGroup>
                <TargetFramework>net48</TargetFramework>
              </PropertyGroup>
            </Project>
            """);
    }

    [Fact]
    public void Analyze_WhenProjectsIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ConfigurationInventoryAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                null!,
                Array.Empty<DiscoveredConfigFile>(),
                ScanFileInventory.Empty));

        Assert.Equal("projects", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenConfigFilesIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ConfigurationInventoryAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                null!,
                ScanFileInventory.Empty));

        Assert.Equal("configFiles", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenFileInventoryIsNull_ThrowsArgumentNullException()
    {
        var analyzer = new ConfigurationInventoryAnalyzer();

        var exception = Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<DiscoveredConfigFile>(),
                null!));

        Assert.Equal("fileInventory", exception.ParamName);
    }

    [Fact]
    public void Analyze_WhenNoEvidenceExists_ReturnsEmptyReport()
    {
        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<DiscoveredConfigFile>(),
            ScanFileInventory.Empty);

        Assert.Empty(report.Findings);
        Assert.Equal(0, report.FindingCount);
        Assert.Equal(0, report.ConfigurationFileCount);
        Assert.Equal(0, report.CategoryCount);
        Assert.Equal(0, report.PotentialMigrationConcernCount);
    }

    [Fact]
    public void Analyze_WhenConfigFileExists_AddsConfigurationFileFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 2,
                ConnectionStringsCount = 1,
                CustomSectionCount = 1
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x =>
            x.Category == ConfigurationInventoryCategory.ConfigurationFile);

        Assert.Equal(ConfigurationInventoryCategory.ConfigurationFile, finding.Category);
        Assert.Equal("Web.config", finding.Name);
        Assert.Equal(ConfigurationInventorySourceType.Configuration, finding.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", finding.SourcePath);
        Assert.Null(finding.ProjectName);
        Assert.Contains("2 app setting(s)", finding.Evidence);
        Assert.Contains("1 connection string(s)", finding.Evidence);
        Assert.Contains("1 custom section(s)", finding.Evidence);
        Assert.Null(finding.MaskedValue);
        Assert.Equal(ConfigurationInventoryConfidence.High, finding.Confidence);
        Assert.True(finding.RequiresReview);
    }

    [Fact]
    public void Analyze_WhenAppSettingExists_AddsAppSettingFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ApiBaseUrl",
                        MaskedValue = "https://api.example.test"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.AppSetting);

        Assert.Equal("ApiBaseUrl", finding.Name);
        Assert.Equal("App setting configured.", finding.Evidence);
        Assert.Equal("https://api.example.test", finding.MaskedValue);
        Assert.Equal(ConfigurationInventoryConfidence.High, finding.Confidence);
        Assert.True(finding.RequiresReview);
    }

    [Fact]
    public void Analyze_WhenSensitiveAppSettingExists_MasksValue()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ApiToken",
                        MaskedValue = "real-token-value"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.AppSetting);

        Assert.Equal("***", finding.MaskedValue);
        Assert.DoesNotContain("real-token-value", finding.MaskedValue);
    }

    [Fact]
    public void Analyze_WhenOnlyAppSettingCountExists_AddsMediumConfidenceAppSettingFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettingsCount = 3
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.AppSetting);

        Assert.Equal("Web.config", finding.Name);
        Assert.Equal("3 app setting(s) configured.", finding.Evidence);
        Assert.Equal(ConfigurationInventoryConfidence.Medium, finding.Confidence);
    }

    [Fact]
    public void Analyze_WhenConnectionStringExists_AddsConnectionStringFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
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

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.ConnectionString);

        Assert.Equal("MainDatabase", finding.Name);
        Assert.Equal(ConfigurationInventorySourceType.Configuration, finding.SourceType);
        Assert.Equal(@"C:\Repo\Web.config", finding.SourcePath);
        Assert.Contains("System.Data.SqlClient", finding.Evidence);
        Assert.Equal("Server=.;Database=Main;User Id=***;Password=***;", finding.MaskedValue);
        Assert.Equal(ConfigurationInventoryConfidence.High, finding.Confidence);
        Assert.True(finding.RequiresReview);
    }

    [Fact]
    public void Analyze_WhenConnectionStringContainsRawSecret_MasksSecret()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                ConnectionStrings =
                {
                    new DiscoveredConnectionString
                    {
                        Name = "MainDatabase",
                        MaskedConnectionString = "Server=.;Database=Main;User Id=admin;Password=SuperSecret123;"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.ConnectionString);

        Assert.Contains("Password=***", finding.MaskedValue);
        Assert.DoesNotContain("SuperSecret123", finding.MaskedValue);
    }

    [Fact]
    public void Analyze_WhenOnlyConnectionStringCountExists_AddsMediumConfidenceConnectionStringFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                ConnectionStringsCount = 2
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.ConnectionString);

        Assert.Equal("Web.config", finding.Name);
        Assert.Equal("2 connection string(s) configured.", finding.Evidence);
        Assert.Equal(ConfigurationInventoryConfidence.Medium, finding.Confidence);
    }

    [Fact]
    public void Analyze_WhenCustomSectionExists_AddsCustomSectionFinding()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                CustomSections =
                {
                    new DiscoveredConfigSection
                    {
                        Name = "legacySettings",
                        Type = "Sample.LegacySettingsSection, Sample"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.CustomSection);

        Assert.Equal("legacySettings", finding.Name);
        Assert.Contains("Sample.LegacySettingsSection", finding.Evidence);
        Assert.Equal(ConfigurationInventoryConfidence.High, finding.Confidence);
    }

    [Fact]
    public void Analyze_WhenWebConfigContainsSystemServiceModel_AddsWcfConfigurationFinding()
    {
        var webConfig = WriteFile(
            "Web.config",
            """
            <configuration>
              <system.serviceModel>
                <services />
              </system.serviceModel>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfig
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.WcfConfiguration);

        Assert.Equal("system.serviceModel", finding.Name);
        Assert.Contains("WCF", finding.Evidence);
    }

    [Fact]
    public void Analyze_WhenWebConfigContainsSystemWeb_AddsAspNetIisConfigurationFinding()
    {
        var webConfig = WriteFile(
            "Web.config",
            """
            <configuration>
              <system.web>
                <authentication mode="Forms" />
              </system.web>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfig
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.AspNetIisConfiguration);

        Assert.Equal("ASP.NET / IIS configuration", finding.Name);
        Assert.Contains("system.web", finding.Evidence);
    }

    [Fact]
    public void Analyze_WhenBindingRedirectExists_AddsBindingRedirectFinding()
    {
        var appConfig = WriteFile(
            "App.config",
            """
            <configuration>
              <runtime>
                <assemblyBinding xmlns="urn:schemas-microsoft-com:asm.v1">
                  <dependentAssembly>
                    <bindingRedirect oldVersion="0.0.0.0-13.0.0.0" newVersion="13.0.0.0" />
                  </dependentAssembly>
                </assemblyBinding>
              </runtime>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = appConfig
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.BindingRedirect);
    }

    [Fact]
    public void Analyze_WhenAuthenticationOrAuthorizationExists_AddsAuthenticationAuthorizationFinding()
    {
        var webConfig = WriteFile(
            "Web.config",
            """
            <configuration>
              <system.web>
                <authentication mode="Forms" />
                <authorization>
                  <deny users="?" />
                </authorization>
              </system.web>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfig
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.AuthenticationAuthorization);
    }

    [Fact]
    public void Analyze_WhenLoggingSectionExists_AddsLoggingDiagnosticsFinding()
    {
        var webConfig = WriteFile(
            "Web.config",
            """
            <configuration>
              <system.diagnostics>
                <trace />
              </system.diagnostics>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfig
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.LoggingDiagnostics);
    }

    [Fact]
    public void Analyze_WhenEntityFrameworkSectionExists_AddsEntityFrameworkConfigurationFinding()
    {
        var webConfig = WriteFile(
            "Web.config",
            """
            <configuration>
              <entityFramework>
                <defaultConnectionFactory type="System.Data.Entity.Infrastructure.SqlConnectionFactory, EntityFramework" />
              </entityFramework>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfig
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.EntityFrameworkConfiguration);
    }

    [Fact]
    public void Analyze_WhenSmtpSectionExists_AddsSmtpMailFinding()
    {
        var webConfig = WriteFile(
            "Web.config",
            """
            <configuration>
              <system.net>
                <mailSettings>
                  <smtp />
                </mailSettings>
              </system.net>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfig
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.SmtpMail);
    }

    [Fact]
    public void Analyze_WhenConfigTransformExists_AddsEnvironmentTransformFinding()
    {
        var transformPath = WriteFile(
            "Web.Release.config",
            """
            <configuration xmlns:xdt="http://schemas.microsoft.com/XML-Document-Transform">
            </configuration>
            """);

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            Array.Empty<DiscoveredConfigFile>(),
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.EnvironmentTransform);

        Assert.Equal("Web.Release.config", finding.Name);
        Assert.Equal(ConfigurationInventorySourceType.Transform, finding.SourceType);
        Assert.Equal(transformPath, finding.SourcePath);
        Assert.Equal("SampleLegacyApp.Web", finding.ProjectName);
    }

    [Fact]
    public void Analyze_WhenAppSettingsJsonExists_AddsFileAndScalarJsonSettingFindings()
    {
        var path = WriteFile(
            "appsettings.Development.json",
            """
            {
              "ConnectionStrings": {
                "RabbitMQ": "amqp://guest:guest@localhost:5672/"
              },
              "RabbitMQ": {
                "HostName": "localhost",
                "Port": 5672,
                "Password": "guest"
              },
              "Logging": {
                "LogLevel": {
                  "Default": "Information"
                }
              }
            }
            """);

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            Array.Empty<DiscoveredConfigFile>(),
            ScanFileInventory.Empty);

        Assert.Contains(
            report.Findings,
            finding =>
                finding.Category == ConfigurationInventoryCategory.JsonConfiguration &&
                finding.Name == "appsettings.Development.json" &&
                finding.SourceType == ConfigurationInventorySourceType.JsonConfiguration &&
                finding.SourcePath == path &&
                finding.MaskedValue is null);

        Assert.Contains(
            report.Findings,
            finding =>
                finding.Category == ConfigurationInventoryCategory.JsonConfiguration &&
                finding.Name == "ConnectionStrings:RabbitMQ" &&
                finding.MaskedValue == "amqp://***:***@localhost:5672/");

        Assert.Contains(
            report.Findings,
            finding =>
                finding.Category == ConfigurationInventoryCategory.JsonConfiguration &&
                finding.Name == "RabbitMQ:HostName" &&
                finding.MaskedValue == "localhost");

        Assert.Contains(
            report.Findings,
            finding =>
                finding.Category == ConfigurationInventoryCategory.JsonConfiguration &&
                finding.Name == "RabbitMQ:Password" &&
                finding.MaskedValue == "***");
    }

    [Fact]
    public void Analyze_WhenSettingsFileExists_AddsSettingsFileFinding()
    {
        var path = WriteFile(
            Path.Combine("Properties", "Settings.settings"),
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <SettingsFile></SettingsFile>
            """);

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            Array.Empty<DiscoveredConfigFile>(),
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.SettingsFile);

        Assert.Equal("Settings.settings", finding.Name);
        Assert.Equal(ConfigurationInventorySourceType.SettingsFile, finding.SourceType);
        Assert.Equal(path, finding.SourcePath);
    }

    [Fact]
    public void Analyze_WhenNuGetConfigExists_AddsBuildPackageConfigurationFinding()
    {
        var path = WriteFile(
            "NuGet.config",
            """
            <configuration>
              <packageSources />
            </configuration>
            """);

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            Array.Empty<DiscoveredConfigFile>(),
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.BuildPackageConfiguration);

        Assert.Equal("NuGet.config", finding.Name);
        Assert.Equal(ConfigurationInventorySourceType.NuGetConfig, finding.SourceType);
        Assert.Equal(path, finding.SourcePath);
    }

    [Fact]
    public void Analyze_WhenConfigurationManagerUsageExists_AddsConfigurationApiUsageFindings()
    {
        var sourceFile = new ScanFile(
            "SampleLegacyApp.Web",
            _projectFilePath,
            _projectDirectory,
            Path.Combine(_projectDirectory, "SettingsReader.cs"),
            "SettingsReader.cs",
            ".cs",
            """
            using System.Configuration;

            public sealed class SettingsReader
            {
                public string? Read()
                {
                    var value = ConfigurationManager.AppSettings["ApiBaseUrl"];
                    var cs = ConfigurationManager.ConnectionStrings["MainDatabase"];
                    return value;
                }
            }
            """);

        var inventory = new ScanFileInventory(
            new[] { sourceFile },
            Array.Empty<ScanFile>(),
            Array.Empty<ScanFile>(),
            Array.Empty<ScanFile>(),
            Array.Empty<string>());

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            Array.Empty<DiscoveredConfigFile>(),
            inventory);

        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.ConfigurationApiUsage && x.Name == "ConfigurationManager.AppSettings");
        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.ConfigurationApiUsage && x.Name == "ConfigurationManager.ConnectionStrings");
    }

    [Fact]
    public void Analyze_WhenIConfigurationUsageExists_AddsConfigurationApiUsageFindings()
    {
        var sourceFile = new ScanFile(
            "SampleLegacyApp.Web",
            _projectFilePath,
            _projectDirectory,
            Path.Combine(_projectDirectory, "OptionsReader.cs"),
            "OptionsReader.cs",
            ".cs",
            """
            using Microsoft.Extensions.Configuration;

            public sealed class OptionsReader
            {
                public OptionsReader(IConfiguration configuration)
                {
                    var section = configuration.GetSection("FeatureFlags");
                }
            }
            """);

        var inventory = new ScanFileInventory(
            new[] { sourceFile },
            Array.Empty<ScanFile>(),
            Array.Empty<ScanFile>(),
            Array.Empty<ScanFile>(),
            Array.Empty<string>());

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            Array.Empty<DiscoveredConfigFile>(),
            inventory);

        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.ConfigurationApiUsage && x.Name == "IConfiguration");
        Assert.Contains(report.Findings, x => x.Category == ConfigurationInventoryCategory.ConfigurationApiUsage && x.Name == "GetSection");
    }

    [Fact]
    public void Analyze_AssignsProjectNameToDiscoveredConfigFileFindings()
    {
        var webConfig = WriteFile(
            "Web.config",
            """
            <configuration>
              <appSettings>
                <add key="ApiBaseUrl" value="https://api.example.test" />
              </appSettings>
              <system.serviceModel>
                <services />
              </system.serviceModel>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfig,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ApiBaseUrl",
                        MaskedValue = "https://api.example.test"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            configFiles,
            ScanFileInventory.Empty);

        Assert.All(
            report.Findings.Where(finding =>
                finding.Category is ConfigurationInventoryCategory.ConfigurationFile
                    or ConfigurationInventoryCategory.AppSetting
                    or ConfigurationInventoryCategory.WcfConfiguration),
            finding => Assert.Equal("SampleLegacyApp.Web", finding.ProjectName));
    }

    [Fact]
    public void Analyze_WhenRabbitMqConnectionStringAppSettingExists_MasksUriCredentialsButKeepsUsefulParts()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "RabbitMQConnectionString",
                        MaskedValue = "amqp://sample-user:sample-password-do-not-use@rabbitmq-dev:5672/sample"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var finding = report.Findings.Single(x => x.Category == ConfigurationInventoryCategory.AppSetting);

        Assert.Equal("amqp://***:***@rabbitmq-dev:5672/sample", finding.MaskedValue);
        Assert.DoesNotContain("sample-user", finding.MaskedValue);
        Assert.DoesNotContain("sample-password-do-not-use", finding.MaskedValue);
    }

    [Fact]
    public void Analyze_DeduplicatesFindings()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ApiBaseUrl",
                        MaskedValue = "https://api.example.test"
                    },
                    new DiscoveredAppSetting
                    {
                        Key = "ApiBaseUrl",
                        MaskedValue = "https://api.example.test"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        Assert.Equal(2, report.Findings.Count);
        Assert.Single(report.Findings.Where(x => x.Category == ConfigurationInventoryCategory.AppSetting));
    }

    [Fact]
    public void Analyze_DoesNotExposeRawSecrets()
    {
        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Repo\Web.config",
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ClientSecret",
                        MaskedValue = "plain-client-secret"
                    }
                },
                ConnectionStrings =
                {
                    new DiscoveredConnectionString
                    {
                        Name = "MainDatabase",
                        MaskedConnectionString = "Server=.;Password=plain-db-password;"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            configFiles,
            ScanFileInventory.Empty);

        var joinedValues = string.Join(
            Environment.NewLine,
            report.Findings.Select(x => x.MaskedValue ?? string.Empty));

        Assert.DoesNotContain("plain-client-secret", joinedValues);
        Assert.DoesNotContain("plain-db-password", joinedValues);
        Assert.Contains("***", joinedValues);
    }


    [Fact]
    public void Analyze_WhenLiteralConfigurationManagerKeysAreUsed_MapsUsageBackToVisibleConfigurationEntries()
    {
        var webConfigPath = WriteFile(
            "Web.config",
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="ApiBaseUrl" value="https://api.example.test" />
              </appSettings>
              <connectionStrings>
                <add name="MainDatabase" connectionString="Server=.;Password=plain-db-password;" providerName="System.Data.SqlClient" />
              </connectionStrings>
            </configuration>
            """);

        WriteFile(
            "SettingsReader.cs",
            """
            using System.Configuration;

            namespace SampleLegacyApp.Web;

            public sealed class SettingsReader
            {
                public string? ApiBaseUrl => ConfigurationManager.AppSettings["ApiBaseUrl"];
                public string? MainDatabase => ConfigurationManager.ConnectionStrings["MainDatabase"]?.ConnectionString;
            }
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfigPath,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "ApiBaseUrl",
                        MaskedValue = "https://api.example.test"
                    }
                },
                ConnectionStrings =
                {
                    new DiscoveredConnectionString
                    {
                        Name = "MainDatabase",
                        ProviderName = "System.Data.SqlClient",
                        MaskedConnectionString = "Server=.;Password=plain-db-password;"
                    }
                }
            }
        };

        var projects = CreateProjects();
        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(projects, configFiles, CreateInventory(projects));

        Assert.Contains(report.SourceUsages, usage =>
            usage.Kind == ConfigurationUsageKind.AppSetting &&
            usage.Key == "ApiBaseUrl" &&
            usage.Resolution == ConfigurationUsageKeyResolution.MatchedVisibleConfigurationEntry &&
            usage.ProjectName == "SampleLegacyApp.Web" &&
            usage.LineNumber > 0 &&
            usage.Evidence.Contains("ConfigurationManager.AppSettings", StringComparison.Ordinal));

        Assert.Contains(report.SourceUsages, usage =>
            usage.Kind == ConfigurationUsageKind.ConnectionString &&
            usage.Key == "MainDatabase" &&
            usage.Resolution == ConfigurationUsageKeyResolution.MatchedVisibleConfigurationEntry &&
            !usage.RequiresReview);

        Assert.Contains(report.KeyReconciliations, reconciliation =>
            reconciliation.Kind == ConfigurationUsageKind.AppSetting &&
            reconciliation.Key == "ApiBaseUrl" &&
            reconciliation.StaticSourceUsage == ConfigurationStaticSourceUsage.Found);
    }

    [Fact]
    public void Analyze_WhenConfigurationManagerGetMethodUsesLiteralKey_DetectsUsage()
    {
        var webConfigPath = WriteFile(
            "Web.config",
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="FeatureXEnabled" value="true" />
              </appSettings>
            </configuration>
            """);

        WriteFile(
            "FeatureReader.cs",
            """
            using System.Configuration;

            namespace SampleLegacyApp.Web;

            public sealed class FeatureReader
            {
                public string? FeatureXEnabled => ConfigurationManager.AppSettings.Get("FeatureXEnabled");
            }
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfigPath,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "FeatureXEnabled",
                        MaskedValue = "true"
                    }
                }
            }
        };

        var projects = CreateProjects();
        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(projects, configFiles, CreateInventory(projects));

        Assert.Contains(report.SourceUsages, usage =>
            usage.Kind == ConfigurationUsageKind.AppSetting &&
            usage.Key == "FeatureXEnabled" &&
            usage.Resolution == ConfigurationUsageKeyResolution.MatchedVisibleConfigurationEntry &&
            usage.Evidence.Contains("AppSettings.Get", StringComparison.Ordinal));
    }

    [Fact]
    public void Analyze_WhenConfigurationKeyIsDynamic_ClassifiesUsageAsRequiresReviewWithoutInventingKey()
    {
        WriteFile(
            "DynamicSettingsReader.cs",
            """
            using System.Configuration;

            namespace SampleLegacyApp.Web;

            public sealed class DynamicSettingsReader
            {
                public string? Read(string key) => ConfigurationManager.AppSettings[key];
            }
            """);

        var projects = CreateProjects();
        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            projects,
            Array.Empty<DiscoveredConfigFile>(),
            CreateInventory(projects));

        var usage = Assert.Single(report.SourceUsages);

        Assert.Equal(ConfigurationUsageKind.AppSetting, usage.Kind);
        Assert.Null(usage.Key);
        Assert.Equal(ConfigurationUsageKeyResolution.DynamicKeyRequiresReview, usage.Resolution);
        Assert.True(usage.RequiresReview);
        Assert.Contains("ConfigurationManager.AppSettings[key]", usage.Evidence);
    }

    [Fact]
    public void Analyze_WhenVisibleConfiguredKeyHasNoStaticSourceUsage_UsesCautiousReconciliationWording()
    {
        var webConfigPath = WriteFile(
            "Web.config",
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="FeatureXEnabled" value="true" />
              </appSettings>
            </configuration>
            """);

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = webConfigPath,
                AppSettings =
                {
                    new DiscoveredAppSetting
                    {
                        Key = "FeatureXEnabled",
                        MaskedValue = "true"
                    }
                }
            }
        };

        var analyzer = new ConfigurationInventoryAnalyzer();

        var report = analyzer.Analyze(
            CreateProjects(),
            configFiles,
            ScanFileInventory.Empty);

        var reconciliation = Assert.Single(report.KeyReconciliations);

        Assert.Equal("FeatureXEnabled", reconciliation.Key);
        Assert.Equal(ConfigurationStaticSourceUsage.NoStaticSourceUsageDetected, reconciliation.StaticSourceUsage);
        Assert.Contains("does not prove the key is unused", reconciliation.Notes);
    }

    private static ScanFileInventory CreateInventory(IReadOnlyCollection<DiscoveredProject> projects)
    {
        return new ScanFileInventoryBuilder().Build(projects);
    }

    private DiscoveredProject[] CreateProjects()
    {
        return
        [
            new DiscoveredProject
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = _projectFilePath,
                TargetFramework = "net48"
            }
        ];
    }

    private string WriteFile(string relativePath, string content)
    {
        var path = Path.Combine(_projectDirectory, relativePath);
        var directory = Path.GetDirectoryName(path);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, content);

        return path;
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}




