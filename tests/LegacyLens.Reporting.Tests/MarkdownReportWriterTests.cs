using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests;

public class MarkdownReportWriterTests
{
    [Fact]
    public void Write_WhenModernisationHintsExist_IncludesModernisationHintsSection()
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString(),
            "discovery-report.md");

        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Legacy.Web",
                ProjectFilePath = @"C:\Code\Legacy.Web\Legacy.Web.csproj",
                TargetFramework = "net48"
            }
        };

        var wcfEndpoints = Array.Empty<WcfEndpoint>();
        var wcfServiceContracts = Array.Empty<WcfServiceContract>();

        var modernisationHints = new List<ModernisationHint>
        {
            new()
            {
                Severity = ModernisationHintSeverity.Risk,
                Area = "Target Framework",
                Finding = "Legacy.Web targets net48",
                Reason = ".NET Framework projects usually need extra assessment before migration to modern .NET."
            }
        };

        var writer = new MarkdownReportWriter();

        writer.Write(
            outputPath,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            modernisationHints,
            Array.Empty<DiscoveredConfigFile>());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Modernisation Hints", markdown);
        Assert.Contains("| Severity | Area | Finding | Reason |", markdown);
        Assert.Contains("| Risk | Target Framework | Legacy.Web targets net48 | .NET Framework projects usually need extra assessment before migration to modern .NET. |", markdown);
    }

    [Fact]
    public void Write_IncludesAssemblyReferences()
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "discovery-report.md");

        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                TargetFramework = "net48",
                AssemblyReferences =
                {
                    "System.Web",
                    "System.Web.Mvc"
                }
            }
        };

        var writer = new MarkdownReportWriter();

        writer.Write(
            outputPath,
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<ModernisationHint>(),
            Array.Empty<DiscoveredConfigFile>());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("- Assembly references discovered: 2", markdown);
        Assert.Contains("## Assembly References", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.Web` |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.Web.Mvc` |", markdown);
    }

    [Fact]
    public void Write_WhenNoAssemblyReferences_IncludesNoneRow()
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "discovery-report.md");

        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "SampleLegacyApp.Contracts",
                ProjectFilePath = @"C:\Code\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj",
                TargetFramework = "net48"
            }
        };

        var writer = new MarkdownReportWriter();

        writer.Write(
            outputPath,
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<ModernisationHint>(),
            Array.Empty<DiscoveredConfigFile>());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Assembly References", markdown);
        Assert.Contains("| None | None |", markdown);
    }

    [Fact]
    public void Write_WhenConfigFilesExist_IncludesConfigurationFilesSection()
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "discovery-report.md");

        var projects = Array.Empty<DiscoveredProject>();

        var configFiles = new List<DiscoveredConfigFile>
        {
            new()
            {
                FilePath = @"C:\Code\Legacy.Web\Web.config",
                AppSettingsCount = 10,
                ConnectionStringsCount = 2,
                CustomSectionCount = 1
            }
        };

        var writer = new MarkdownReportWriter();

        writer.Write(
            outputPath,
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<ModernisationHint>(),
            configFiles);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Configuration Files", markdown);
        Assert.Contains("| Config File | App Settings | Connection Strings | Custom Sections |", markdown);
        Assert.Contains(@"| `C:\Code\Legacy.Web\Web.config` | 10 | 2 | 1 |", markdown);
    }

    [Fact]
    public void Write_WhenNoConfigFilesExist_IncludesConfigurationFilesNoneRow()
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "discovery-report.md");

        var writer = new MarkdownReportWriter();

        writer.Write(
            outputPath,
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<ModernisationHint>(),
            Array.Empty<DiscoveredConfigFile>());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Configuration Files", markdown);
        Assert.Contains("| None | 0 | 0 | 0 |", markdown);
    }
}