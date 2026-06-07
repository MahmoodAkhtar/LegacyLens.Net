using LegacyLens.Core.Analysis;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class UpgradeBlockersMarkdownReportWriterTests : IDisposable
{
    private readonly string _tempDirectory;

    public UpgradeBlockersMarkdownReportWriterTests()
    {
        _tempDirectory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.UpgradeBlockersMarkdownReportWriterTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_tempDirectory);
    }

    [Fact]
    public void Write_CreatesUpgradeBlockersReport()
    {
        var outputPath = Path.Combine(_tempDirectory, "output", "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        Assert.True(File.Exists(outputPath));

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# Upgrade Blockers", markdown);
    }

    [Fact]
    public void Write_IncludesRequestedUpgradeTarget_WhenProvided()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Requested upgrade target | net8.0 |", markdown);
        Assert.Contains("| Analysis mode | Static / no-build |", markdown);
        Assert.Contains("| Compatibility guarantee | No |", markdown);
    }

    [Fact]
    public void Write_UsesGeneralWording_WhenUpgradeTargetIsMissing()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport(null);

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Requested upgrade target | General upgrade-blocker review |", markdown);
    }

    [Fact]
    public void Write_IncludesBlockerOverview()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Blocker Overview", markdown);
        Assert.Contains("| Priority | Blocker | Impact | Evidence Count |", markdown);
        Assert.Contains("| 1 | Legacy ASP.NET / System.Web | High | 2 |", markdown);
        Assert.Contains("| 2 | WCF / ServiceModel | High | 1 |", markdown);
        Assert.Contains("| 3 | Package Management | Medium | 1 |", markdown);
    }

    [Fact]
    public void Write_IncludesUpgradeBlockersAndDecisions()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Upgrade Blockers and Decisions", markdown);
        Assert.Contains("| Priority | Area | Blocker / Decision | Impact | Evidence |", markdown);
        Assert.Contains(
            "| 1 | Legacy ASP.NET / System.Web | Migration decision required for classic ASP.NET / System.Web usage. | High | Possible blocker: System.Web assembly reference indicates classic ASP.NET / System.Web usage.; Possible blocker: WebForms page may require replacement or redesign. |",
            markdown);
        Assert.Contains(
            "| 2 | WCF / ServiceModel | Migration decision required for WCF service boundaries and bindings. | High | Possible blocker: WCF endpoint basicHttpBinding requires service boundary and binding review. |",
            markdown);
    }

    [Fact]
    public void Write_IncludesBlockerDetails()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Blocker Details", markdown);
        Assert.Contains("### Legacy ASP.NET / System.Web", markdown);
        Assert.Contains("### WCF / ServiceModel", markdown);
        Assert.Contains("### Package Management", markdown);
        Assert.Contains("Why this matters:", markdown);
        Assert.Contains("Evidence:", markdown);
        Assert.Contains("Decision required:", markdown);
    }

    [Fact]
    public void Write_IncludesEvidenceRows()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("| Project | File / Reference | Finding |", markdown);
        Assert.Contains(
            "| SampleLegacyApp.Web | `C:\\Code\\SampleLegacyApp.Web\\SampleLegacyApp.Web.csproj` | Possible blocker: System.Web assembly reference indicates classic ASP.NET / System.Web usage. |",
            markdown);
        Assert.Contains(
            "| SampleLegacyApp.Web | `C:\\Code\\SampleLegacyApp.Web\\Default.aspx` | Possible blocker: WebForms page may require replacement or redesign. |",
            markdown);
        Assert.Contains(
            "| SampleLegacyApp.Web | `C:\\Code\\SampleLegacyApp.Web\\Web.config` | Possible blocker: WCF endpoint basicHttpBinding requires service boundary and binding review. |",
            markdown);
    }

    [Fact]
    public void Write_IncludesDecisionsRequired()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("- Can the existing web host remain temporarily on .NET Framework?", markdown);
        Assert.Contains("- Should endpoints be migrated gradually to ASP.NET Core?", markdown);
        Assert.Contains("- Should WCF services remain on .NET Framework temporarily?", markdown);
        Assert.Contains("- Should packages.config projects be migrated to PackageReference?", markdown);
    }

    [Fact]
    public void Write_IncludesSuggestedReviewOrder()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Suggested Review Order", markdown);
        Assert.Contains("1. Review Legacy ASP.NET / System.Web blockers first.", markdown);
        Assert.Contains("2. Review WCF / ServiceModel blockers and service boundary decisions.", markdown);
        Assert.Contains("3. Review EF6, EDMX, and data access migration decisions.", markdown);
        Assert.Contains("4. Review package management and package version evidence.", markdown);
        Assert.Contains("5. Review direct assembly references and local/vendor dependency concerns.", markdown);
        Assert.Contains("6. Review configuration and runtime coupling.", markdown);
    }

    [Fact]
    public void Write_IncludesNotesAndLimitations()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("## Notes and Limitations", markdown);
        Assert.Contains("- This report is based on static discovery only.", markdown);
        Assert.Contains("- LegacyLens.NET did not build the solution.", markdown);
        Assert.Contains("- LegacyLens.NET did not run the application or tests.", markdown);
        Assert.Contains("- LegacyLens.NET did not restore NuGet packages.", markdown);
        Assert.Contains("- LegacyLens.NET did not resolve transitive dependencies.", markdown);
        Assert.Contains("- LegacyLens.NET did not inspect NuGet package assets.", markdown);
        Assert.Contains("- LegacyLens.NET did not automatically migrate code.", markdown);
        Assert.Contains("- LegacyLens.NET did not prove that migration is impossible.", markdown);
        Assert.Contains("- A blocker means “requires review”, not “cannot be upgraded”.", markdown);
        Assert.Contains("- Findings should be verified by the development team before migration decisions are made.", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownPipes()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = new UpgradeBlockersReport
        {
            RequestedUpgradeTarget = "net8.0",
            Blockers = new[]
            {
                new UpgradeBlocker
                {
                    Priority = 1,
                    Category = UpgradeBlockerCategory.UnknownRequiresManualReview,
                    Impact = UpgradeBlockerImpact.Unknown,
                    Title = "Review A | B decision.",
                    WhyItMatters = "Finding A | B may affect upgrade planning.",
                    DecisionsRequired = new[]
                    {
                        "Decide whether A | B should be replaced."
                    },
                    Evidence = new[]
                    {
                        new UpgradeBlockerEvidence
                        {
                            ProjectName = "Project | One",
                            Source = @"C:\Code\Project|One\Project.csproj",
                            Finding = "Possible blocker: A | B evidence found."
                        }
                    }
                }
            }
        };

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("Review A \\| B decision.", markdown);
        Assert.Contains("Finding A \\| B may affect upgrade planning.", markdown);
        Assert.Contains("Decide whether A \\| B should be replaced.", markdown);
        Assert.Contains("Project \\| One", markdown);
        Assert.Contains(@"C:\Code\Project\|One\Project.csproj", markdown);
        Assert.Contains("Possible blocker: A \\| B evidence found.", markdown);
    }

    [Fact]
    public void Write_UsesUnknownForMissingOptionalEvidenceValues()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var report = new UpgradeBlockersReport
        {
            RequestedUpgradeTarget = "net8.0",
            Blockers = new[]
            {
                new UpgradeBlocker
                {
                    Priority = 1,
                    Category = UpgradeBlockerCategory.UnknownRequiresManualReview,
                    Impact = UpgradeBlockerImpact.Unknown,
                    Title = "No visible MVP upgrade blocker matched the current static rules.",
                    WhyItMatters = "Static discovery did not find one of the known blocker categories.",
                    DecisionsRequired = new[]
                    {
                        "Review the codebase manually before making upgrade decisions."
                    },
                    Evidence = new[]
                    {
                        new UpgradeBlockerEvidence
                        {
                            Source = "Static analysis summary",
                            Finding = "No configured MVP blocker rule matched the discovered evidence."
                        }
                    }
                }
            }
        };

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains(
            "| unknown | `Static analysis summary` | No configured MVP blocker rule matched the discovered evidence. |",
            markdown);
    }

    [Fact]
    public void Write_CreatesOutputDirectory_WhenItDoesNotExist()
    {
        var outputPath = Path.Combine(
            _tempDirectory,
            "nested",
            "output",
            "upgrade-blockers.md");

        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        writer.Write(outputPath, report);

        Assert.True(File.Exists(outputPath));
    }

    [Fact]
    public void Write_ThrowsArgumentException_WhenOutputPathIsEmpty()
    {
        var report = CreateSampleReport("net8.0");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        Assert.Throws<ArgumentException>(() => writer.Write("", report));
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenReportIsNull()
    {
        var outputPath = Path.Combine(_tempDirectory, "upgrade-blockers.md");

        var writer = new UpgradeBlockersMarkdownReportWriter();

        Assert.Throws<ArgumentNullException>(() => writer.Write(outputPath, null!));
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    private static UpgradeBlockersReport CreateSampleReport(string? requestedUpgradeTarget)
    {
        return new UpgradeBlockersReport
        {
            RequestedUpgradeTarget = requestedUpgradeTarget,
            Blockers = new[]
            {
                new UpgradeBlocker
                {
                    Priority = 1,
                    Category = UpgradeBlockerCategory.LegacyAspNetSystemWeb,
                    Impact = UpgradeBlockerImpact.High,
                    Title = "Migration decision required for classic ASP.NET / System.Web usage.",
                    WhyItMatters = "ASP.NET Core uses a different hosting model and request pipeline. Legacy System.Web, WebForms, ASMX, ASHX, Global.asax, HTTP modules, and HTTP handlers may require redesign, replacement, or staged migration.",
                    DecisionsRequired = new[]
                    {
                        "Can the existing web host remain temporarily on .NET Framework?",
                        "Should endpoints be migrated gradually to ASP.NET Core?",
                        "Are there WebForms, ASMX, ASHX, module, or handler artifacts that need replacement?"
                    },
                    Evidence = new[]
                    {
                        new UpgradeBlockerEvidence
                        {
                            ProjectName = "SampleLegacyApp.Web",
                            Source = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                            Finding = "Possible blocker: System.Web assembly reference indicates classic ASP.NET / System.Web usage."
                        },
                        new UpgradeBlockerEvidence
                        {
                            ProjectName = "SampleLegacyApp.Web",
                            Source = @"C:\Code\SampleLegacyApp.Web\Default.aspx",
                            Finding = "Possible blocker: WebForms page may require replacement or redesign."
                        }
                    }
                },
                new UpgradeBlocker
                {
                    Priority = 2,
                    Category = UpgradeBlockerCategory.WcfServiceModel,
                    Impact = UpgradeBlockerImpact.High,
                    Title = "Migration decision required for WCF service boundaries and bindings.",
                    WhyItMatters = "WCF service hosting, bindings, metadata, security, behaviours, and generated clients may need replacement, compatibility planning, or isolation before moving to modern .NET.",
                    DecisionsRequired = new[]
                    {
                        "Should WCF services remain on .NET Framework temporarily?",
                        "Should service boundaries move to ASP.NET Core APIs, gRPC, queues, or another integration style?",
                        "Are SOAP clients, metadata exchange endpoints, bindings, security, and timeouts externally depended on?"
                    },
                    Evidence = new[]
                    {
                        new UpgradeBlockerEvidence
                        {
                            ProjectName = "SampleLegacyApp.Web",
                            Source = @"C:\Code\SampleLegacyApp.Web\Web.config",
                            Finding = "Possible blocker: WCF endpoint basicHttpBinding requires service boundary and binding review."
                        }
                    }
                },
                new UpgradeBlocker
                {
                    Priority = 3,
                    Category = UpgradeBlockerCategory.PackageManagement,
                    Impact = UpgradeBlockerImpact.Medium,
                    Title = "Package management migration decision required.",
                    WhyItMatters = "Legacy packages.config usage, missing versions, or mismatched package target framework metadata can complicate package restore, upgrade sequencing, and dependency review.",
                    DecisionsRequired = new[]
                    {
                        "Should packages.config projects be migrated to PackageReference?",
                        "Are package versions centrally managed elsewhere?",
                        "Do package target framework values reflect the current project targets?"
                    },
                    Evidence = new[]
                    {
                        new UpgradeBlockerEvidence
                        {
                            ProjectName = "SampleLegacyApp.Data",
                            Source = @"C:\Code\SampleLegacyApp.Data\packages.config",
                            Finding = "Possible blocker: EntityFramework uses legacy packages.config package management."
                        }
                    }
                }
            }
        };
    }
}