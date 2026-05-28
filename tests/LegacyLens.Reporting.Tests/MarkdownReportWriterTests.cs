using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests;

public sealed class MarkdownReportWriterTests : IDisposable
{
    private readonly string _rootPath;

    public MarkdownReportWriterTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Reporting.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void Write_CreatesMarkdownReport_WithUpdatedModernisationHintsAndWcfEndpointDetails()
    {
        var outputPath = Path.Combine(_rootPath, "output", "discovery-report.md");

        var solutionPath = Path.Combine(_rootPath, "SampleLegacyApp.sln");
        var contractsProjectPath = Path.Combine(_rootPath, "SampleLegacyApp.Contracts", "SampleLegacyApp.Contracts.csproj");
        var webProjectPath = Path.Combine(_rootPath, "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj");
        var configPath = Path.Combine(_rootPath, "SampleLegacyApp.Web", "Web.config");
        var contractSourcePath = Path.Combine(_rootPath, "SampleLegacyApp.Contracts", "CustomerContracts.cs");

        var solutions = new List<DiscoveredSolution>
        {
            new()
            {
                Name = "SampleLegacyApp",
                SolutionFilePath = solutionPath,
                ProjectFilePaths = new List<string>
                {
                    contractsProjectPath,
                    webProjectPath
                }
            }
        };

        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "SampleLegacyApp.Contracts",
                ProjectFilePath = contractsProjectPath,
                TargetFramework = "net48"
            },
            new()
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = webProjectPath,
                TargetFramework = "net48",
                ProjectReferences = new List<string>
                {
                    @"..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj"
                },
                PackageReferences = new List<string>
                {
                    "System.ServiceModel.Http",
                    "Newtonsoft.Json"
                },
                AssemblyReferences = new List<string>
                {
                    "System.Web",
                    "System.Web.Mvc"
                }
            }
        };

        var wcfEndpoints = new List<WcfEndpoint>
        {
            new()
            {
                ConfigFilePath = configPath,
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                Address = "",
                Binding = "basicHttpBinding",
                Contract = "SampleLegacyApp.Contracts.ICustomerService",
                BindingConfiguration = "CustomerBinding",
                SecurityMode = "Transport",
                TransportClientCredentialType = "Windows",
                MessageClientCredentialType = "UserName",
                IsMetadataExchangeEndpoint = true
            }
        };

        var wcfServiceContracts = new List<WcfServiceContract>
        {
            new()
            {
                Name = "ICustomerService",
                SourceFilePath = contractSourcePath,
                Operations = new List<string>
                {
                    "GetCustomer"
                }
            }
        };

        var configFiles = new List<DiscoveredConfigFile>
        {
            new()
            {
                FilePath = configPath,
                AppSettingsCount = 10,
                ConnectionStringsCount = 1,
                CustomSectionCount = 1
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var modernisationHints = analyzer.Analyze(
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            configFiles);

        var writer = new MarkdownReportWriter();

        writer.Write(
            outputPath,
            solutions,
            projects,
            wcfEndpoints,
            wcfServiceContracts,
            modernisationHints,
            configFiles);

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("# LegacyLens.NET Discovery Report", markdown);

        Assert.Contains("- Solutions discovered: 1", markdown);
        Assert.Contains("- Projects discovered: 2", markdown);
        Assert.Contains("- Project references discovered: 1", markdown);
        Assert.Contains("- Package references discovered: 2", markdown);
        Assert.Contains("- WCF endpoints discovered: 1", markdown);
        Assert.Contains("- WCF service contracts discovered: 1", markdown);
        Assert.Contains("- Assembly references discovered: 2", markdown);

        Assert.Contains("## Solutions", markdown);
        Assert.Contains($"| SampleLegacyApp | 2 | `{solutionPath}` |", markdown);

        Assert.Contains("## Projects", markdown);
        Assert.Contains($"| SampleLegacyApp.Contracts | net48 | `{contractsProjectPath}` |", markdown);
        Assert.Contains($"| SampleLegacyApp.Web | net48 | `{webProjectPath}` |", markdown);

        Assert.Contains("## Project Dependency Diagram", markdown);
        Assert.Contains("```mermaid", markdown);
        Assert.Contains("graph TD", markdown);
        Assert.Contains("SampleLegacyApp_Web --> SampleLegacyApp_Contracts", markdown);

        Assert.Contains("## Project References", markdown);
        Assert.Contains(
            @"| SampleLegacyApp.Web | `..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj` |",
            markdown);

        Assert.Contains("## Assembly References", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.Web` |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.Web.Mvc` |", markdown);

        Assert.Contains("## Package References", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `Newtonsoft.Json` |", markdown);
        Assert.Contains("| SampleLegacyApp.Web | `System.ServiceModel.Http` |", markdown);

        Assert.Contains("## WCF Endpoints", markdown);
        Assert.Contains(
            "| Service | Address | Binding | Binding Configuration | Security Mode | Transport Credential | Message Credential | Metadata Exchange | Contract | Config File |",
            markdown);
        Assert.Contains(
            $"| SampleLegacyApp.Services.CustomerService |  | basicHttpBinding | CustomerBinding | Transport | Windows | UserName | True | SampleLegacyApp.Contracts.ICustomerService | `{configPath}` |",
            markdown);

        Assert.Contains("## WCF Service Contracts", markdown);
        Assert.Contains(
            $"| ICustomerService | GetCustomer | `{contractSourcePath}` |",
            markdown);

        Assert.Contains("## Configuration Files", markdown);
        Assert.Contains(
            $"| `{configPath}` | 10 | 1 | 1 |",
            markdown);

        Assert.Contains("## Modernisation Hints", markdown);

        Assert.Contains(
            "| Risk | Target Framework | SampleLegacyApp.Contracts targets net48 | .NET Framework projects usually need extra assessment before migration to modern .NET. |",
            markdown);

        Assert.Contains(
            "| Risk | Target Framework | SampleLegacyApp.Web targets net48 | .NET Framework projects usually need extra assessment before migration to modern .NET. |",
            markdown);

        Assert.Contains(
            "| Risk | Packages | SampleLegacyApp.Web references System.ServiceModel.Http | System.ServiceModel packages indicate WCF-related usage, which is important for modernisation planning. |",
            markdown);

        Assert.Contains(
            "| Risk | WCF | 1 WCF endpoint(s) discovered | Configured WCF endpoints usually represent service boundaries or integration points that need migration assessment. |",
            markdown);

        Assert.Contains(
            "| Risk | WCF | 1 WCF service contract(s) discovered | WCF service contracts identify service APIs that may need redesign, replacement, or compatibility planning. |",
            markdown);

        Assert.Contains(
            "| Warning | WCF Binding | basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService | basicHttpBinding commonly indicates SOAP interoperability that may need replacement or compatibility planning. |",
            markdown);

        Assert.Contains(
            "| Info | WCF Configuration | SampleLegacyApp.Services.CustomerService uses binding configuration CustomerBinding | Named WCF binding configurations may contain security, timeout, size, protocol, or credential settings that need migration review. |",
            markdown);

        Assert.Contains(
            "| Warning | WCF Security | SampleLegacyApp.Services.CustomerService uses WCF security mode Transport | WCF security settings need explicit review when replacing WCF endpoints with modern HTTP, JSON, gRPC, or other service endpoints. |",
            markdown);

        Assert.Contains(
            "| Warning | WCF Security | SampleLegacyApp.Services.CustomerService uses transport credential type Windows | Transport credential settings may affect authentication and hosting choices during service migration. |",
            markdown);

        Assert.Contains(
            "| Info | WCF Metadata | SampleLegacyApp.Services.CustomerService exposes a metadata exchange endpoint | Metadata exchange endpoints are useful discovery signals when identifying SOAP contracts and generated client dependencies. |",
            markdown);

        Assert.Contains(
            "| Risk | Legacy ASP.NET | SampleLegacyApp.Web references System.Web | System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core. |",
            markdown);

        Assert.Contains(
            "| Warning | Legacy ASP.NET | SampleLegacyApp.Web references System.Web.Mvc | System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment. |",
            markdown);

        Assert.Contains(
            "| Warning | Configuration | Web.config contains 10 appSettings entries | A large number of appSettings entries may indicate environment-specific behaviour or operational settings hidden in configuration. |",
            markdown);

        Assert.Contains(
            "| Info | Configuration | Web.config contains 1 connection string(s) | Connection strings identify external data dependencies that should be reviewed during migration planning. |",
            markdown);

        Assert.Contains(
            "| Warning | Configuration | Web.config contains 1 custom configuration section(s) | Custom configuration sections may indicate framework-specific or application-specific behaviour that needs migration assessment. |",
            markdown);
    }

    [Fact]
    public void Write_CreatesMarkdownReport_WithNoneRows_WhenNoItemsAreDiscovered()
    {
        var outputPath = Path.Combine(_rootPath, "output", "empty-discovery-report.md");

        var writer = new MarkdownReportWriter();

        writer.Write(
            outputPath,
            Array.Empty<DiscoveredSolution>(),
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<ModernisationHint>(),
            Array.Empty<DiscoveredConfigFile>());

        var markdown = File.ReadAllText(outputPath);

        Assert.Contains("- Solutions discovered: 0", markdown);
        Assert.Contains("- Projects discovered: 0", markdown);
        Assert.Contains("- Project references discovered: 0", markdown);
        Assert.Contains("- Package references discovered: 0", markdown);
        Assert.Contains("- WCF endpoints discovered: 0", markdown);
        Assert.Contains("- WCF service contracts discovered: 0", markdown);
        Assert.Contains("- Assembly references discovered: 0", markdown);

        Assert.Contains("| None | 0 | None |", markdown);
        Assert.Contains("| None | None |", markdown);
        Assert.Contains("| None | None | None | None | None | None | None | None | None | None |", markdown);
        Assert.Contains("| None | None | None |", markdown);
        Assert.Contains("| None | 0 | 0 | 0 |", markdown);
        Assert.Contains("| None | None | None | None |", markdown);
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

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}