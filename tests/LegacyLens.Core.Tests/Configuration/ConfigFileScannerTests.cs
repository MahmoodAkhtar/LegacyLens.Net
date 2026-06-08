using LegacyLens.Core.Configuration;

namespace LegacyLens.Core.Tests.Configuration;

public sealed class ConfigFileScannerTests : IDisposable
{
    private readonly string _tempDirectory;

    public ConfigFileScannerTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.ConfigFileScannerTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Scan_WhenAppConfigContainsAppSettings_ReturnsAppSettingsCountAndAppSettingDetails()
    {
        var configPath = Path.Combine(_tempDirectory, "app.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="SettingOne" value="ValueOne" />
                <add key="SettingTwo" value="ValueTwo" />
              </appSettings>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);

        Assert.Equal(configPath, result.FilePath);
        Assert.Equal(2, result.AppSettingsCount);
        Assert.Equal(0, result.ConnectionStringsCount);
        Assert.Equal(0, result.CustomSectionCount);

        Assert.Collection(
            result.AppSettings,
            appSetting =>
            {
                Assert.Equal("SettingOne", appSetting.Key);
                Assert.Equal("ValueOne", appSetting.MaskedValue);
            },
            appSetting =>
            {
                Assert.Equal("SettingTwo", appSetting.Key);
                Assert.Equal("ValueTwo", appSetting.MaskedValue);
            });

        Assert.Empty(result.ConnectionStrings);
        Assert.Empty(result.CustomSections);
    }

    [Fact]
    public void Scan_WhenAppSettingKeyLooksSensitive_MasksAppSettingValue()
    {
        var configPath = Path.Combine(_tempDirectory, "app.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="ApiToken" value="real-token-value" />
              </appSettings>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);
        var appSetting = Assert.Single(result.AppSettings);

        Assert.Equal("ApiToken", appSetting.Key);
        Assert.Equal("***", appSetting.MaskedValue);
        Assert.DoesNotContain("real-token-value", appSetting.MaskedValue);
    }

    [Fact]
    public void Scan_WhenAppSettingValueContainsSensitiveQueryString_MasksSensitiveQueryStringValue()
    {
        var configPath = Path.Combine(_tempDirectory, "app.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="StorageCallbackUrl" value="https://example.test/callback?token=secret-token&amp;name=value" />
              </appSettings>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);
        var appSetting = Assert.Single(result.AppSettings);

        Assert.Equal("StorageCallbackUrl", appSetting.Key);
        Assert.Equal("https://example.test/callback?token=***&name=value", appSetting.MaskedValue);
        Assert.DoesNotContain("secret-token", appSetting.MaskedValue);
    }

    [Fact]
    public void Scan_WhenAppSettingValueContainsUrlCredentials_MasksCredentials()
    {
        var configPath = Path.Combine(_tempDirectory, "app.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="FeedUrl" value="https://user:real-password@example.test/packages" />
              </appSettings>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);
        var appSetting = Assert.Single(result.AppSettings);

        Assert.Equal("FeedUrl", appSetting.Key);
        Assert.Equal("https://***:***@example.test/packages", appSetting.MaskedValue);
        Assert.DoesNotContain("user", appSetting.MaskedValue);
        Assert.DoesNotContain("real-password", appSetting.MaskedValue);
    }

    [Fact]
    public void Scan_WhenWebConfigContainsConnectionStrings_ReturnsConnectionStringsCountAndConnectionStringDetails()
    {
        var configPath = Path.Combine(_tempDirectory, "web.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <connectionStrings>
                <add name="MainDatabase"
                     connectionString="Server=.;Database=Main;"
                     providerName="System.Data.SqlClient" />
                <add name="AuditDatabase"
                     connectionString="Server=.;Database=Audit;"
                     providerName="Microsoft.Data.SqlClient" />
              </connectionStrings>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);

        Assert.Equal(configPath, result.FilePath);
        Assert.Equal(0, result.AppSettingsCount);
        Assert.Equal(2, result.ConnectionStringsCount);
        Assert.Equal(0, result.CustomSectionCount);

        Assert.Collection(
            result.ConnectionStrings,
            connectionString =>
            {
                Assert.Equal("MainDatabase", connectionString.Name);
                Assert.Equal("System.Data.SqlClient", connectionString.ProviderName);
                Assert.Equal("Server=.;Database=Main;", connectionString.MaskedConnectionString);
            },
            connectionString =>
            {
                Assert.Equal("AuditDatabase", connectionString.Name);
                Assert.Equal("Microsoft.Data.SqlClient", connectionString.ProviderName);
                Assert.Equal("Server=.;Database=Audit;", connectionString.MaskedConnectionString);
            });

        Assert.Empty(result.AppSettings);
        Assert.Empty(result.CustomSections);
    }

    [Fact]
    public void Scan_WhenConnectionStringContainsSensitiveValues_MasksSensitiveValues()
    {
        var configPath = Path.Combine(_tempDirectory, "web.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <connectionStrings>
                <add name="MainDatabase"
                     connectionString="Server=.;Database=Main;User Id=admin;Password=real-password;Application Name=LegacyApp;"
                     providerName="System.Data.SqlClient" />
              </connectionStrings>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);
        var connectionString = Assert.Single(result.ConnectionStrings);

        Assert.Equal("MainDatabase", connectionString.Name);
        Assert.Equal("System.Data.SqlClient", connectionString.ProviderName);
        Assert.Equal(
            "Server=.;Database=Main;User Id=***;Password=***;Application Name=LegacyApp;",
            connectionString.MaskedConnectionString);
        Assert.DoesNotContain("admin", connectionString.MaskedConnectionString);
        Assert.DoesNotContain("real-password", connectionString.MaskedConnectionString);
    }

    [Fact]
    public void Scan_WhenConfigContainsCustomSections_ReturnsCustomSectionCountAndCustomSectionDetails()
    {
        var configPath = Path.Combine(_tempDirectory, "web.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <configSections>
                <section name="customSettings" type="Legacy.CustomSettingsSection, Legacy" />
                <sectionGroup name="applicationSettings">
                  <section name="Legacy.Properties.Settings" type="System.Configuration.ClientSettingsSection, System" />
                </sectionGroup>
              </configSections>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);

        Assert.Equal(configPath, result.FilePath);
        Assert.Equal(0, result.AppSettingsCount);
        Assert.Equal(0, result.ConnectionStringsCount);
        Assert.Equal(3, result.CustomSectionCount);

        Assert.Collection(
            result.CustomSections,
            customSection =>
            {
                Assert.Equal("customSettings", customSection.Name);
                Assert.Equal("Legacy.CustomSettingsSection, Legacy", customSection.Type);
            },
            customSection =>
            {
                Assert.Equal("applicationSettings", customSection.Name);
                Assert.Null(customSection.Type);
            },
            customSection =>
            {
                Assert.Equal("Legacy.Properties.Settings", customSection.Name);
                Assert.Equal("System.Configuration.ClientSettingsSection, System", customSection.Type);
            });

        Assert.Empty(result.AppSettings);
        Assert.Empty(result.ConnectionStrings);
    }

    [Fact]
    public void Scan_WhenConfigEntriesAreMissingNamesOrKeys_IgnoresUnnamedDetailsButKeepsValidDetails()
    {
        var configPath = Path.Combine(_tempDirectory, "web.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add value="MissingKey" />
                <add key="ValidSetting" value="ValidValue" />
              </appSettings>
              <connectionStrings>
                <add connectionString="Server=.;Database=MissingName;" />
                <add name="ValidDatabase" connectionString="Server=.;Database=Valid;" />
              </connectionStrings>
              <configSections>
                <section type="Missing.Name, Missing" />
                <section name="validSection" type="Valid.Section, Valid" />
              </configSections>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        var result = Assert.Single(results);

        Assert.Equal(1, result.AppSettingsCount);
        Assert.Equal(1, result.ConnectionStringsCount);
        Assert.Equal(1, result.CustomSectionCount);

        var appSetting = Assert.Single(result.AppSettings);
        Assert.Equal("ValidSetting", appSetting.Key);
        Assert.Equal("ValidValue", appSetting.MaskedValue);

        var connectionString = Assert.Single(result.ConnectionStrings);
        Assert.Equal("ValidDatabase", connectionString.Name);
        Assert.Equal("Server=.;Database=Valid;", connectionString.MaskedConnectionString);

        var customSection = Assert.Single(result.CustomSections);
        Assert.Equal("validSection", customSection.Name);
        Assert.Equal("Valid.Section, Valid", customSection.Type);
    }

    [Fact]
    public void Scan_WhenConfigFileIsInvalid_IgnoresFile()
    {
        var configPath = Path.Combine(_tempDirectory, "web.config");

        File.WriteAllText(
            configPath,
            """
            <configuration>
              <appSettings>
                <add key="Broken" value="Missing close tags" />
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        Assert.Empty(results);
    }

    [Fact]
    public void Scan_WhenConfigFileIsNotAppConfigOrWebConfig_IgnoresFile()
    {
        var configPath = Path.Combine(_tempDirectory, "custom.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <appSettings>
                <add key="SettingOne" value="ValueOne" />
              </appSettings>
            </configuration>
            """);

        var scanner = new ConfigFileScanner();

        var results = scanner.Scan(_tempDirectory);

        Assert.Empty(results);
    }

    [Fact]
    public void Scan_WhenRootPathIsEmpty_ThrowsArgumentException()
    {
        var scanner = new ConfigFileScanner();

        Assert.Throws<ArgumentException>(() => scanner.Scan(""));
    }

    [Fact]
    public void Scan_WhenRootPathDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        var scanner = new ConfigFileScanner();

        var missingPath = Path.Combine(_tempDirectory, "missing");

        Assert.Throws<DirectoryNotFoundException>(() => scanner.Scan(missingPath));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }
}