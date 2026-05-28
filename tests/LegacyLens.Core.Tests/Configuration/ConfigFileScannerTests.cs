
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
    public void Scan_WhenAppConfigContainsAppSettings_ReturnsAppSettingsCount()
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
    }

    [Fact]
    public void Scan_WhenWebConfigContainsConnectionStrings_ReturnsConnectionStringsCount()
    {
        var configPath = Path.Combine(_tempDirectory, "web.config");

        File.WriteAllText(
            configPath,
            """
            <?xml version="1.0" encoding="utf-8" ?>
            <configuration>
              <connectionStrings>
                <add name="MainDatabase" connectionString="Server=.;Database=Main;" />
                <add name="AuditDatabase" connectionString="Server=.;Database=Audit;" />
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
    }

    [Fact]
    public void Scan_WhenConfigContainsCustomSections_ReturnsCustomSectionCount()
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