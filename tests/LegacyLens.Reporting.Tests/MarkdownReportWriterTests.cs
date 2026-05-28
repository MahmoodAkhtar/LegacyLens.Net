using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests;

public sealed class MarkdownReportWriterTests
{
    [Fact]
    public void Write_CreatesMarkdownReport()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains("# LegacyLens.NET Discovery Report", markdown);
    }

    [Fact]
    public void Write_IncludesSummaryCounts()
    {
        var markdown = WriteReport(
            solutions: new List<DiscoveredSolution>
            {
                new()
                {
                    Name = "SampleLegacyApp",
                    SolutionFilePath = @"C:\Code\SampleLegacyApp\SampleLegacyApp.sln",
                    ProjectFilePaths =
                    {
                        @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj"
                    }
                }
            },
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    ProjectReferences =
                    {
                        @"..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj"
                    },
                    PackageReferences =
                    {
                        "Newtonsoft.Json"
                    },
                    AssemblyReferences =
                    {
                        "System.Web"
                    }
                }
            },
            wcfEndpoints: new List<WcfEndpoint>
            {
                new()
                {
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Address = "",
                    Binding = "basicHttpBinding",
                    Contract = "SampleLegacyApp.Contracts.ICustomerService",
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config"
                }
            },
            wcfServiceContracts: new List<WcfServiceContract>
            {
                new()
                {
                    Name = "ICustomerService",
                    SourceFilePath = @"C:\Code\SampleLegacyApp.Contracts\CustomerContracts.cs",
                    Operations =
                    {
                        "GetCustomer"
                    }
                }
            });

        Assert.Contains("- Solutions discovered: 1", markdown);
        Assert.Contains("- Projects discovered: 1", markdown);
        Assert.Contains("- Project references discovered: 1", markdown);
        Assert.Contains("- Package references discovered: 1", markdown);
        Assert.Contains("- WCF endpoints discovered: 1", markdown);
        Assert.Contains("- WCF service contracts discovered: 1", markdown);
        Assert.Contains("- Assembly references discovered: 1", markdown);
    }

    [Fact]
    public void Write_IncludesSolutions()
    {
        var markdown = WriteReport(
            solutions: new List<DiscoveredSolution>
            {
                new()
                {
                    Name = "SampleLegacyApp",
                    SolutionFilePath = @"C:\Code\SampleLegacyApp\SampleLegacyApp.sln",
                    ProjectFilePaths =
                    {
                        @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                        @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj"
                    }
                }
            });

        Assert.Contains("## Solutions", markdown);
        Assert.Contains("| Solution | Projects | Solution File |", markdown);
        Assert.Contains("| SampleLegacyApp | 2 | `C:\\Code\\SampleLegacyApp\\SampleLegacyApp.sln` |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoSolutionsExist()
    {
        var markdown = WriteReport();

        Assert.Contains("## Solutions", markdown);
        Assert.Contains("| None | 0 | None |", markdown);
    }

    [Fact]
    public void Write_IncludesProjects()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains("## Projects", markdown);
        Assert.Contains("| Project | Target Framework | Project File |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | net48 | `C:\\Code\\SampleLegacyApp.Web\\SampleLegacyApp.Web.csproj` |", markdown);
    }

    [Fact]
    public void Write_IncludesUnknownTargetFramework_WhenProjectTargetFrameworkIsMissing()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Legacy",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Legacy\SampleLegacyApp.Legacy.csproj"
                }
            });

        Assert.Contains("| SampleLegacyApp.Legacy | Unknown | `C:\\Code\\SampleLegacyApp.Legacy\\SampleLegacyApp.Legacy.csproj` |", markdown);
    }

    [Fact]
    public void Write_IncludesTargetFrameworkSummary()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                },
                new()
                {
                    Name = "SampleLegacyApp.Services",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
                    TargetFramework = "net48"
                },
                new()
                {
                    Name = "SampleLegacyApp.ModernApi",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.ModernApi\SampleLegacyApp.ModernApi.csproj",
                    TargetFramework = "net8.0"
                }
            });

        Assert.Contains("## Target Framework Summary", markdown);
        Assert.Contains("| Target Framework | Projects |", markdown);
        Assert.Contains("| net48 | 2 |", markdown);
        Assert.Contains("| net8.0 | 1 |", markdown);
    }

    [Fact]
    public void Write_GroupsUnknownTargetFrameworks_WhenTargetFrameworkIsMissing()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.LegacyA",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.LegacyA\SampleLegacyApp.LegacyA.csproj"
                },
                new()
                {
                    Name = "SampleLegacyApp.LegacyB",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.LegacyB\SampleLegacyApp.LegacyB.csproj",
                    TargetFramework = ""
                }
            });

        Assert.Contains("## Target Framework Summary", markdown);
        Assert.Contains("| Unknown | 2 |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRowInTargetFrameworkSummary_WhenNoProjectsExist()
    {
        var markdown = WriteReport();

        Assert.Contains("## Target Framework Summary", markdown);
        Assert.Contains("| None | 0 |", markdown);
    }

    [Fact]
    public void Write_IncludesPackageReferenceSummary()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "EntityFramework",
                        "Newtonsoft.Json"
                    }
                },
                new()
                {
                    Name = "SampleLegacyApp.Data",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "EntityFramework",
                        "Dapper"
                    }
                }
            });

        Assert.Contains("## Package Reference Summary", markdown);
        Assert.Contains("| Package | Projects |", markdown);
        Assert.Contains("| Dapper | 1 |", markdown);
        Assert.Contains("| EntityFramework | 2 |", markdown);
        Assert.Contains("| Newtonsoft.Json | 1 |", markdown);
    }

    [Fact]
    public void Write_CountsPackageReferenceSummaryOncePerProject()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "Newtonsoft.Json",
                        "Newtonsoft.Json"
                    }
                }
            });

        Assert.Contains("## Package Reference Summary", markdown);
        Assert.Contains("| Newtonsoft.Json | 1 |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRowInPackageReferenceSummary_WhenNoPackagesExist()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains("## Package Reference Summary", markdown);
        Assert.Contains("| None | 0 |", markdown);
    }

    [Fact]
    public void Write_IncludesProjectDependencyDiagram()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    ProjectReferences =
                    {
                        @"..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj"
                    }
                },
                new()
                {
                    Name = "SampleLegacyApp.Services",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains("## Project Dependency Diagram", markdown);
        Assert.Contains("```mermaid", markdown);
        Assert.Contains("graph TD", markdown);
        Assert.Contains("SampleLegacyApp_Web --> SampleLegacyApp_Services", markdown);
    }

    [Fact]
    public void Write_IncludesProjectReferences()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    ProjectReferences =
                    {
                        @"..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj"
                    }
                }
            });

        Assert.Contains("## Project References", markdown);
        Assert.Contains("| From | To |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `..\\SampleLegacyApp.Services\\SampleLegacyApp.Services.csproj` |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoProjectReferencesExist()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains("## Project References", markdown);
        Assert.Contains("| None | None |", markdown);
    }

    [Fact]
    public void Write_IncludesAssemblyReferences()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
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
            });

        Assert.Contains("## Assembly References", markdown);
        Assert.Contains("| Project | Assembly |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.Web` |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.Web.Mvc` |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoAssemblyReferencesExist()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains("## Assembly References", markdown);
        Assert.Contains("| None | None |", markdown);
    }

    [Fact]
    public void Write_IncludesPackageReferences()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "Newtonsoft.Json",
                        "System.ServiceModel.Http"
                    }
                }
            });

        Assert.Contains("## Package References", markdown);
        Assert.Contains("| Project | Package |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `Newtonsoft.Json` |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.ServiceModel.Http` |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoPackageReferencesExist()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains("## Package References", markdown);
        Assert.Contains("| None | None |", markdown);
    }

    [Fact]
    public void Write_IncludesWcfEndpoints()
    {
        var markdown = WriteReport(
            wcfEndpoints: new List<WcfEndpoint>
            {
                new()
                {
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Address = "",
                    Binding = "basicHttpBinding",
                    BindingConfiguration = "CustomerBinding",
                    SecurityMode = "Transport",
                    TransportClientCredentialType = "Windows",
                    MessageClientCredentialType = "UserName",
                    IsMetadataExchangeEndpoint = false,
                    Contract = "SampleLegacyApp.Contracts.ICustomerService",
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config"
                }
            });

        Assert.Contains("## WCF Endpoints", markdown);
        Assert.Contains("| Service | Address | Binding | Binding Configuration | Security Mode | Transport Credential | Message Credential | Metadata Exchange | Contract | Config File |", markdown);
        Assert.Contains("| SampleLegacyApp.Services.CustomerService |  | basicHttpBinding | CustomerBinding | Transport | Windows | UserName | False | SampleLegacyApp.Contracts.ICustomerService | `C:\\Code\\SampleLegacyApp.Web\\Web.config` |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoWcfEndpointsExist()
    {
        var markdown = WriteReport();

        Assert.Contains("## WCF Endpoints", markdown);
        Assert.Contains("| None | None | None | None | None | None | None | None | None | None |", markdown);
    }

    [Fact]
    public void Write_IncludesWcfServiceContracts()
    {
        var markdown = WriteReport(
            wcfServiceContracts: new List<WcfServiceContract>
            {
                new()
                {
                    Name = "ICustomerService",
                    SourceFilePath = @"C:\Code\SampleLegacyApp.Contracts\CustomerContracts.cs",
                    Operations =
                    {
                        "GetCustomer",
                        "SaveCustomer"
                    }
                }
            });

        Assert.Contains("## WCF Service Contracts", markdown);
        Assert.Contains("| Contract | Operations | Source File |", markdown);
        Assert.Contains("| ICustomerService | GetCustomer, SaveCustomer | `C:\\Code\\SampleLegacyApp.Contracts\\CustomerContracts.cs` |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoWcfServiceContractsExist()
    {
        var markdown = WriteReport();

        Assert.Contains("## WCF Service Contracts", markdown);
        Assert.Contains("| None | None | None |", markdown);
    }

    [Fact]
    public void Write_IncludesConfigurationFiles()
    {
        var markdown = WriteReport(
            configFiles: new List<DiscoveredConfigFile>
            {
                new()
                {
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    AppSettingsCount = 12,
                    ConnectionStringsCount = 2,
                    CustomSectionCount = 1
                }
            });

        Assert.Contains("## Configuration Files", markdown);
        Assert.Contains("| Config File | App Settings | Connection Strings | Custom Sections |", markdown);
        Assert.Contains("| `C:\\Code\\SampleLegacyApp.Web\\Web.config` | 12 | 2 | 1 |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoConfigurationFilesExist()
    {
        var markdown = WriteReport();

        Assert.Contains("## Configuration Files", markdown);
        Assert.Contains("| None | 0 | 0 | 0 |", markdown);
    }

    [Fact]
    public void Write_IncludesModernisationHints()
    {
        var markdown = WriteReport(
            modernisationHints: new List<ModernisationHint>
            {
                new()
                {
                    Severity = ModernisationHintSeverity.Risk,
                    Area = "Target Framework",
                    Finding = "SampleLegacyApp.Web targets net48",
                    Reason = ".NET Framework projects usually need extra assessment before migration to modern .NET."
                }
            });

        Assert.Contains("## Modernisation Hints", markdown);
        Assert.Contains("| Severity | Area | Finding | Reason |", markdown);
        Assert.Contains("| Risk | Target Framework | SampleLegacyApp.Web targets net48 | .NET Framework projects usually need extra assessment before migration to modern .NET. |", markdown);
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoModernisationHintsExist()
    {
        var markdown = WriteReport();

        Assert.Contains("## Modernisation Hints", markdown);
        Assert.Contains("| None | None | None | None |", markdown);
    }

    [Fact]
    public void Write_EscapesMarkdownTablePipes()
    {
        var markdown = WriteReport(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "Sample|Legacy|Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "Package|With|Pipes"
                    },
                    AssemblyReferences =
                    {
                        "Assembly|With|Pipes"
                    }
                }
            },
            modernisationHints: new List<ModernisationHint>
            {
                new()
                {
                    Severity = ModernisationHintSeverity.Warning,
                    Area = "Area|With|Pipes",
                    Finding = "Finding|With|Pipes",
                    Reason = "Reason|With|Pipes"
                }
            });

        Assert.Contains("Sample\\|Legacy\\|Web", markdown);
        Assert.Contains("Package\\|With\\|Pipes", markdown);
        Assert.Contains("Assembly\\|With\\|Pipes", markdown);
        Assert.Contains("Area\\|With\\|Pipes", markdown);
        Assert.Contains("Finding\\|With\\|Pipes", markdown);
        Assert.Contains("Reason\\|With\\|Pipes", markdown);
    }

    [Fact]
    public void Write_CreatesOutputDirectory_WhenItDoesNotExist()
    {
        var rootDirectory = CreateTemporaryDirectory();
        var outputPath = Path.Combine(rootDirectory, "nested", "reports", "discovery-report.md");

        try
        {
            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            Assert.True(File.Exists(outputPath));
        }
        finally
        {
            DeleteDirectory(rootDirectory);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentException_WhenOutputPathIsEmpty()
    {
        var writer = new MarkdownReportWriter();

        Assert.Throws<ArgumentException>(() =>
            writer.Write(
                "",
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenSolutionsIsNull()
    {
        var writer = new MarkdownReportWriter();

        Assert.Throws<ArgumentNullException>(() =>
            writer.Write(
                "report.md",
                null!,
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenProjectsIsNull()
    {
        var writer = new MarkdownReportWriter();

        Assert.Throws<ArgumentNullException>(() =>
            writer.Write(
                "report.md",
                Array.Empty<DiscoveredSolution>(),
                null!,
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenWcfEndpointsIsNull()
    {
        var writer = new MarkdownReportWriter();

        Assert.Throws<ArgumentNullException>(() =>
            writer.Write(
                "report.md",
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                null!,
                Array.Empty<WcfServiceContract>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenWcfServiceContractsIsNull()
    {
        var writer = new MarkdownReportWriter();

        Assert.Throws<ArgumentNullException>(() =>
            writer.Write(
                "report.md",
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                null!,
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenModernisationHintsIsNull()
    {
        var writer = new MarkdownReportWriter();

        Assert.Throws<ArgumentNullException>(() =>
            writer.Write(
                "report.md",
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                null!,
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenConfigFilesIsNull()
    {
        var writer = new MarkdownReportWriter();

        Assert.Throws<ArgumentNullException>(() =>
            writer.Write(
                "report.md",
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<ModernisationHint>(),
                null!));
    }

    private static string WriteReport(
        IReadOnlyList<DiscoveredSolution>? solutions = null,
        IReadOnlyList<DiscoveredProject>? projects = null,
        IReadOnlyList<WcfEndpoint>? wcfEndpoints = null,
        IReadOnlyList<WcfServiceContract>? wcfServiceContracts = null,
        IReadOnlyList<ModernisationHint>? modernisationHints = null,
        IReadOnlyList<DiscoveredConfigFile>? configFiles = null)
    {
        var rootDirectory = CreateTemporaryDirectory();
        var outputPath = Path.Combine(rootDirectory, "discovery-report.md");

        try
        {
            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                solutions ?? Array.Empty<DiscoveredSolution>(),
                projects ?? Array.Empty<DiscoveredProject>(),
                wcfEndpoints ?? Array.Empty<WcfEndpoint>(),
                wcfServiceContracts ?? Array.Empty<WcfServiceContract>(),
                modernisationHints ?? Array.Empty<ModernisationHint>(),
                configFiles ?? Array.Empty<DiscoveredConfigFile>());

            return File.ReadAllText(outputPath);
        }
        finally
        {
            DeleteDirectory(rootDirectory);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var directory = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Reporting.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(directory);

        return directory;
    }

    private static void DeleteDirectory(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, recursive: true);
        }
    }
}