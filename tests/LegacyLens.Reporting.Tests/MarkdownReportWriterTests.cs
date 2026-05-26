using LegacyLens.Core.Analysis;
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
            modernisationHints);

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
            Array.Empty<ModernisationHint>());

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
            Array.Empty<ModernisationHint>());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Assembly References", markdown);
        Assert.Contains("| None | None |", markdown);
    }
}