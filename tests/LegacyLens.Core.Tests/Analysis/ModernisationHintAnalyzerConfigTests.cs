using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerConfigTests
{
    [Fact]
    public void Analyze_WhenConfigFileHasManyAppSettings_AddsConfigurationWarningHint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var configFiles = new List<DiscoveredConfigFile>
        {
            new()
            {
                FilePath = @"C:\Code\Legacy.Web\Web.config",
                AppSettingsCount = 10,
                ConnectionStringsCount = 0,
                CustomSectionCount = 0
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            configFiles);

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Warning &&
            hint.Area == "Configuration" &&
            hint.Finding.Contains("Web.config") &&
            hint.Finding.Contains("10 appSettings"));
    }

    [Fact]
    public void Analyze_WhenConfigFileHasConnectionStrings_AddsConfigurationInfoHint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var configFiles = new List<DiscoveredConfigFile>
        {
            new()
            {
                FilePath = @"C:\Code\Legacy.Web\Web.config",
                AppSettingsCount = 0,
                ConnectionStringsCount = 2,
                CustomSectionCount = 0
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            configFiles);

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Info &&
            hint.Area == "Configuration" &&
            hint.Finding.Contains("Web.config") &&
            hint.Finding.Contains("2 connection string"));
    }

    [Fact]
    public void Analyze_WhenConfigFileHasCustomSections_AddsConfigurationWarningHint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var configFiles = new List<DiscoveredConfigFile>
        {
            new()
            {
                FilePath = @"C:\Code\Legacy.Web\Web.config",
                AppSettingsCount = 0,
                ConnectionStringsCount = 0,
                CustomSectionCount = 1
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            configFiles);

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Warning &&
            hint.Area == "Configuration" &&
            hint.Finding.Contains("Web.config") &&
            hint.Finding.Contains("1 custom configuration section"));
    }
}