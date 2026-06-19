using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class UpgradeReadinessMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public UpgradeReadinessMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.UpgradeReadinessMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_CreatesUpgradeReadinessReport()
    {
        var outputPath = Path.Combine(_tempDirectory, "output", "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        Assert.True(File.Exists(outputPath));

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Upgrade Readiness Report", markdown);
    }

    [Fact]
    public void Write_IncludesRequestedUpgradeTarget_WhenProvided()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Requested upgrade target | net8.0 |", markdown);
        Assert.Contains("| Analysis mode | Static / no-build |", markdown);
        Assert.Contains("| Compatibility guarantee | No |", markdown);
    }

    [Fact]
    public void Write_UsesGeneralWording_WhenUpgradeTargetIsMissing()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport(null);

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Requested upgrade target | General upgrade-readiness review |", markdown);
    }

    [Fact]
    public void Write_IncludesCurrentProjectTargets()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Current Project Targets", markdown);
        Assert.Contains("| Project | Target Framework | Project File |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | net48 | `C:\\Code\\SampleLegacyApp.Web\\SampleLegacyApp.Web.csproj` |", markdown);
        Assert.Contains("| SampleLegacyApp.Data | net48 | `C:\\Code\\SampleLegacyApp.Data\\SampleLegacyApp.Data.csproj` |", markdown);
    }

    [Fact]
    public void Write_IncludesUpgradeReadinessOverview()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Upgrade Readiness Overview", markdown);
        Assert.Contains("| Target frameworks | Requires review | `.NET Framework projects detected` |", markdown);
        Assert.Contains("| Legacy ASP.NET | Possible blocker | `System.Web or legacy ASP.NET artifacts detected` |", markdown);
        Assert.Contains("| WCF | Requires review | `System.ServiceModel or WCF configuration evidence detected` |", markdown);
    }

    [Fact]
    public void Write_IncludesProjectUpgradeCandidates()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Project Upgrade Candidates", markdown);
        Assert.Contains("| Project | Current Target | Readiness | Reason |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | net48 | Higher risk / review first | Legacy ASP.NET, System.Web, or web runtime evidence detected. Review before attempting a modern .NET upgrade. |", markdown);
        Assert.Contains("| SampleLegacyApp.Data | net48 | Moderate review required | Static evidence found package, WCF, EF6, packages.config, or direct assembly considerations. |", markdown);
    }

    [Fact]
    public void Write_IncludesPossibleUpgradeConcerns()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Possible Upgrade Concerns", markdown);
        Assert.Contains("| .NET Framework target framework | `SampleLegacyApp.Web (net48), SampleLegacyApp.Data (net48)` | Requires review before moving to modern .NET. |", markdown);
        Assert.Contains("| Legacy ASP.NET runtime | `System.Web or legacy ASP.NET artifact evidence found` | ASP.NET Core does not use the System.Web request pipeline. |", markdown);
        Assert.Contains("| WCF usage | `1 endpoint(s), 1 service contract(s), and System.ServiceModel evidence where present` | WCF service boundaries, bindings, metadata, and clients need migration decisions. |", markdown);
    }

    [Fact]
    public void Write_IncludesPackageUpgradeConsiderations()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Package Upgrade Considerations", markdown);
        Assert.Contains("| Project | Package | Version | Project Target | Package Target | Source Format | Source Path | Possible Concern |", markdown);
        Assert.Contains("| SampleLegacyApp.Data | EntityFramework | 6.4.4 | net48 | net48 | packages.config | `C:\\Code\\SampleLegacyApp.Data\\packages.config` | Classic Entity Framework should be reviewed before migration to EF Core or modern .NET. |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | System.ServiceModel.Http | unknown | net48 |  | PackageReference | `C:\\Code\\SampleLegacyApp.Web\\SampleLegacyApp.Web.csproj` | WCF-related package. Review WCF usage and replacement strategy before upgrading. |", markdown);
    }

    [Fact]
    public void Write_IncludesAssemblyReferenceConsiderations()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Assembly Reference Considerations", markdown);
        Assert.Contains("| Project | Assembly | Source Project | Possible Concern |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | System.Web | `C:\\Code\\SampleLegacyApp.Web\\SampleLegacyApp.Web.csproj` | Legacy ASP.NET assembly reference. ASP.NET Core does not use the System.Web request pipeline. |", markdown);
        Assert.Contains("| SampleLegacyApp.Services | System.ServiceModel | `C:\\Code\\SampleLegacyApp.Services\\SampleLegacyApp.Services.csproj` | WCF assembly reference. WCF migration or compatibility strategy requires review. |", markdown);
    }

    [Fact]
    public void Write_IncludesConfigurationAndRuntimeConsiderations()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Configuration and Runtime Considerations", markdown);
        Assert.Contains("| Source | Finding | Possible Upgrade Concern |", markdown);
        Assert.Contains("| `C:\\Code\\SampleLegacyApp.Web\\Web.config` | appSettings: 2, connection strings: 1, custom sections: 1 | Configuration values may represent runtime behaviour or external dependencies that need migration review. |", markdown);
        Assert.Contains("| `WCF discovery` | 1 endpoint(s), 1 service contract(s), 1 behaviour(s) | WCF runtime configuration and service boundaries may need migration or compatibility planning. |", markdown);
    }

    [Fact]
    public void Write_IncludesSuggestedReviewOrder()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Suggested Review Order", markdown);
        Assert.Contains("1. Review lower-risk class library candidates first.", markdown);
        Assert.Contains("2. Review package management style and direct package concerns.", markdown);
        Assert.Contains("3. Review data access projects and EF6 usage.", markdown);
        Assert.Contains("4. Review WCF/service boundaries.", markdown);
        Assert.Contains("5. Review web host, startup, routing, request pipeline, and configuration-heavy projects last.", markdown);
    }

    [Fact]
    public void Write_IncludesStaticAnalysisLimitations()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Notes and Limitations", markdown);
        Assert.Contains("- This report is based on static discovery only.", markdown);
        Assert.Contains("- LegacyLens.NET did not build the solution.", markdown);
        Assert.Contains("- LegacyLens.NET did not run the application or tests.", markdown);
        Assert.Contains("- LegacyLens.NET did not restore NuGet packages.", markdown);
        Assert.Contains("- LegacyLens.NET did not resolve transitive dependencies.", markdown);
        Assert.Contains("- LegacyLens.NET did not inspect NuGet package assets.", markdown);
        Assert.Contains("- Findings should be verified by the development team before migration decisions are made.", markdown);
    }

    [Fact]
    public void Write_EscapesPipeCharactersInMarkdownTableValues()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = new UpgradeReadinessReport
        {
            RequestedUpgradeTarget = "net8.0",
            Overview = new[]
            {
                new UpgradeReadinessOverviewItem
                {
                    Area = "Configuration",
                    Status = "Requires review",
                    Evidence = "Value contains A | B"
                }
            },
            ProjectReadiness = new[]
            {
                new ProjectUpgradeReadiness
                {
                    ProjectName = "Project|WithPipe",
                    CurrentTargetFramework = "net48",
                    ProjectFilePath = @"C:\Code\ProjectWithPipe\ProjectWithPipe.csproj",
                    Readiness = UpgradeReadinessLevel.ModerateReviewRequired,
                    Reason = "Reason contains A | B"
                }
            },
            Concerns = Array.Empty<UpgradeConcern>(),
            PackageConsiderations = Array.Empty<PackageUpgradeConsideration>(),
            AssemblyConsiderations = Array.Empty<AssemblyUpgradeConsideration>(),
            ConfigurationRuntimeConsiderations = Array.Empty<ConfigurationRuntimeConsideration>()
        };

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Project\\|WithPipe", markdown);
        Assert.Contains("Value contains A \\| B", markdown);
        Assert.Contains("Reason contains A \\| B", markdown);
    }

    [Fact]
    public void Write_UsesUnknownForMissingOptionalValues()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-readiness-report.md");

        var report = new UpgradeReadinessReport
        {
            ProjectReadiness = new[]
            {
                new ProjectUpgradeReadiness
                {
                    ProjectName = "SampleLegacyApp.Unknown",
                    CurrentTargetFramework = null,
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Unknown\SampleLegacyApp.Unknown.csproj",
                    Readiness = UpgradeReadinessLevel.Unknown,
                    Reason = "No target framework was discovered."
                }
            },
            Overview = Array.Empty<UpgradeReadinessOverviewItem>(),
            Concerns = Array.Empty<UpgradeConcern>(),
            PackageConsiderations = new[]
            {
                new PackageUpgradeConsideration
                {
                    ProjectName = "SampleLegacyApp.Unknown",
                    PackageName = "Unknown.Package",
                    Version = null,
                    ProjectTargetFramework = null,
                    PackageTargetFramework = null,
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Unknown\SampleLegacyApp.Unknown.csproj",
                    PossibleConcern = "Package version was not found."
                }
            },
            AssemblyConsiderations = Array.Empty<AssemblyUpgradeConsideration>(),
            ConfigurationRuntimeConsiderations = Array.Empty<ConfigurationRuntimeConsideration>()
        };

        var writer = new UpgradeReadinessMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| SampleLegacyApp.Unknown | unknown | `C:\\Code\\SampleLegacyApp.Unknown\\SampleLegacyApp.Unknown.csproj` |", markdown);
        Assert.Contains("| SampleLegacyApp.Unknown | Unknown.Package | unknown | unknown |  | PackageReference | `C:\\Code\\SampleLegacyApp.Unknown\\SampleLegacyApp.Unknown.csproj` | Package version was not found. |", markdown);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static UpgradeReadinessReport CreateSampleReport(string? requestedUpgradeTarget)
    {
        return new UpgradeReadinessReport
        {
            RequestedUpgradeTarget = requestedUpgradeTarget,
            Overview = new[]
            {
                new UpgradeReadinessOverviewItem
                {
                    Area = "Target frameworks",
                    Status = "Requires review",
                    Evidence = ".NET Framework projects detected"
                },
                new UpgradeReadinessOverviewItem
                {
                    Area = "Legacy ASP.NET",
                    Status = "Possible blocker",
                    Evidence = "System.Web or legacy ASP.NET artifacts detected"
                },
                new UpgradeReadinessOverviewItem
                {
                    Area = "WCF",
                    Status = "Requires review",
                    Evidence = "System.ServiceModel or WCF configuration evidence detected"
                }
            },
            ProjectReadiness = new[]
            {
                new ProjectUpgradeReadiness
                {
                    ProjectName = "SampleLegacyApp.Web",
                    CurrentTargetFramework = "net48",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    Readiness = UpgradeReadinessLevel.HigherRiskReviewFirst,
                    Reason = "Legacy ASP.NET, System.Web, or web runtime evidence detected. Review before attempting a modern .NET upgrade."
                },
                new ProjectUpgradeReadiness
                {
                    ProjectName = "SampleLegacyApp.Data",
                    CurrentTargetFramework = "net48",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
                    Readiness = UpgradeReadinessLevel.ModerateReviewRequired,
                    Reason = "Static evidence found package, WCF, EF6, packages.config, or direct assembly considerations."
                }
            },
            Concerns = new[]
            {
                new UpgradeConcern
                {
                    Concern = ".NET Framework target framework",
                    Evidence = "SampleLegacyApp.Web (net48), SampleLegacyApp.Data (net48)",
                    WhyItMatters = "Requires review before moving to modern .NET."
                },
                new UpgradeConcern
                {
                    Concern = "Legacy ASP.NET runtime",
                    Evidence = "System.Web or legacy ASP.NET artifact evidence found",
                    WhyItMatters = "ASP.NET Core does not use the System.Web request pipeline."
                },
                new UpgradeConcern
                {
                    Concern = "WCF usage",
                    Evidence = "1 endpoint(s), 1 service contract(s), and System.ServiceModel evidence where present",
                    WhyItMatters = "WCF service boundaries, bindings, metadata, and clients need migration decisions."
                }
            },
            PackageConsiderations = new[]
            {
                new PackageUpgradeConsideration
                {
                    ProjectName = "SampleLegacyApp.Data",
                    PackageName = "EntityFramework",
                    Version = "6.4.4",
                    ProjectTargetFramework = "net48",
                    PackageTargetFramework = "net48",
                    SourceFormat = "packages.config",
                    SourcePath = @"C:\Code\SampleLegacyApp.Data\packages.config",
                    PossibleConcern = "Classic Entity Framework should be reviewed before migration to EF Core or modern .NET."
                },
                new PackageUpgradeConsideration
                {
                    ProjectName = "SampleLegacyApp.Web",
                    PackageName = "System.ServiceModel.Http",
                    Version = null,
                    ProjectTargetFramework = "net48",
                    PackageTargetFramework = null,
                    SourceFormat = "PackageReference",
                    SourcePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    PossibleConcern = "WCF-related package. Review WCF usage and replacement strategy before upgrading."
                }
            },
            AssemblyConsiderations = new[]
            {
                new AssemblyUpgradeConsideration
                {
                    ProjectName = "SampleLegacyApp.Web",
                    AssemblyName = "System.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    PossibleConcern = "Legacy ASP.NET assembly reference. ASP.NET Core does not use the System.Web request pipeline."
                },
                new AssemblyUpgradeConsideration
                {
                    ProjectName = "SampleLegacyApp.Services",
                    AssemblyName = "System.ServiceModel",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
                    PossibleConcern = "WCF assembly reference. WCF migration or compatibility strategy requires review."
                }
            },
            ConfigurationRuntimeConsiderations = new[]
            {
                new ConfigurationRuntimeConsideration
                {
                    Source = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    Finding = "appSettings: 2, connection strings: 1, custom sections: 1",
                    PossibleConcern = "Configuration values may represent runtime behaviour or external dependencies that need migration review."
                },
                new ConfigurationRuntimeConsideration
                {
                    Source = "WCF discovery",
                    Finding = "1 endpoint(s), 1 service contract(s), 1 behaviour(s)",
                    PossibleConcern = "WCF runtime configuration and service boundaries may need migration or compatibility planning."
                }
            }
        };
    }
}
