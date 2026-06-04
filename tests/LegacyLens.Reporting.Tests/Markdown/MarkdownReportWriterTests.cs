using FluentAssertions;
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;
using LegacyLens.Reporting.Markdown;

namespace LegacyLens.Reporting.Tests.Markdown;

public sealed class MarkdownReportWriterTests
{
    [Fact]
    public void Write_CreatesMarkdownReport()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "output", "discovery-report.md");

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            File.Exists(outputPath).Should().BeTrue();

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("# LegacyLens.NET Discovery Report");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesSummaryCounts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var solution = new DiscoveredSolution
            {
                Name = "SampleLegacyApp",
                SolutionFilePath = Path.Combine(rootPath, "SampleLegacyApp.sln"),
                ProjectFilePaths =
                {
                    Path.Combine(rootPath, "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj")
                }
            };

            var project = new DiscoveredProject
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = Path.Combine(rootPath, "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj"),
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
            };

            var wcfEndpoint = new WcfEndpoint
            {
                ConfigFilePath = Path.Combine(rootPath, "Web.config"),
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                Address = "",
                Binding = "basicHttpBinding",
                Contract = "SampleLegacyApp.Contracts.ICustomerService"
            };

            var wcfServiceContract = new WcfServiceContract
            {
                Name = "ICustomerService",
                SourceFilePath = Path.Combine(rootPath, "CustomerContracts.cs"),
                Operations =
                {
                    "GetCustomer"
                }
            };

            var legacyAspNetArtifact = new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.WebFormsPage,
                Name = "Default.aspx",
                FilePath = Path.Combine(rootPath, "Default.aspx")
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                new[] { solution },
                new[] { project },
                new[] { wcfEndpoint },
                new[] { wcfServiceContract },
                new[] { legacyAspNetArtifact },
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("- Solutions discovered: 1");
            markdown.Should().Contain("- Projects discovered: 1");
            markdown.Should().Contain("- Project references discovered: 1");
            markdown.Should().Contain("- Package references discovered: 1");
            markdown.Should().Contain("- WCF endpoints discovered: 1");
            markdown.Should().Contain("- WCF service contracts discovered: 1");
            markdown.Should().Contain("- Legacy ASP.NET artifacts discovered: 1");
            markdown.Should().Contain("- Assembly references discovered: 1");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesSolutionsSection()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var solutionFilePath = Path.Combine(rootPath, "SampleLegacyApp.sln");
            var projectFilePath = Path.Combine(rootPath, "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var solution = new DiscoveredSolution
            {
                Name = "SampleLegacyApp",
                SolutionFilePath = solutionFilePath,
                ProjectFilePaths =
                {
                    projectFilePath
                }
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                new[] { solution },
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## Solutions");
            markdown.Should().Contain("| Solution | Projects | Solution File |");
            markdown.Should().Contain($"| SampleLegacyApp | 1 | `{solutionFilePath}` |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesProjectAndDependencySections()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var projectFilePath = Path.Combine(rootPath, "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var project = new DiscoveredProject
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = projectFilePath,
                TargetFramework = "net48",
                ProjectReferences =
                {
                    @"..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj"
                },
                AssemblyReferences =
                {
                    "System.Web"
                },
                PackageReferences =
                {
                    "Newtonsoft.Json"
                }
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                new[] { project },
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## Projects");
            markdown.Should().Contain($"| SampleLegacyApp.Web | net48 | `{projectFilePath}` |");

            markdown.Should().Contain("## Target Framework Summary");
            markdown.Should().Contain("| net48 | 1 |");

            markdown.Should().Contain("## Package Reference Summary");
            markdown.Should().Contain("| Newtonsoft.Json | 1 |");

            markdown.Should().Contain("## Project References");
            markdown.Should()
                .Contain(@"| SampleLegacyApp.Web | `..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj` |");

            markdown.Should().Contain("## Assembly References");
            markdown.Should().Contain("| SampleLegacyApp.Web | `System.Web` |");

            markdown.Should().Contain("## Package References");
            markdown.Should().Contain("| SampleLegacyApp.Web | `Newtonsoft.Json` |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesProjectDependencyDiagram()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var webProjectPath = Path.Combine(rootPath, "SampleLegacyApp.Web", "SampleLegacyApp.Web.csproj");
            var servicesProjectPath =
                Path.Combine(rootPath, "SampleLegacyApp.Services", "SampleLegacyApp.Services.csproj");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var webProject = new DiscoveredProject
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = webProjectPath,
                TargetFramework = "net48",
                ProjectReferences =
                {
                    @"..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj"
                }
            };

            var servicesProject = new DiscoveredProject
            {
                Name = "SampleLegacyApp.Services",
                ProjectFilePath = servicesProjectPath,
                TargetFramework = "net48"
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                new[] { webProject, servicesProject },
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## Project Dependency Diagram");
            markdown.Should().Contain("```mermaid");
            markdown.Should().Contain("graph TD");
            markdown.Should().Contain("SampleLegacyApp_Web --> SampleLegacyApp_Services");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesWcfEndpointDetails()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var configFilePath = Path.Combine(rootPath, "Web.config");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var endpoint = new WcfEndpoint
            {
                ConfigFilePath = configFilePath,
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                Address = "",
                Binding = "basicHttpBinding",
                BindingConfiguration = "CustomerBinding",
                Contract = "SampleLegacyApp.Contracts.ICustomerService",
                SecurityMode = "Transport",
                TransportClientCredentialType = "Windows",
                MessageClientCredentialType = "UserName",
                IsMetadataExchangeEndpoint = false,
                OpenTimeout = "00:01:00",
                CloseTimeout = "00:01:00",
                SendTimeout = "00:02:00",
                ReceiveTimeout = "00:10:00",
                MaxReceivedMessageSize = "1048576",
                MaxBufferSize = "65536",
                MaxBufferPoolSize = "524288",
                TransferMode = "Streamed",
                ReaderQuotaMaxDepth = "32",
                ReaderQuotaMaxStringContentLength = "8192",
                ReaderQuotaMaxArrayLength = "16384",
                ReaderQuotaMaxBytesPerRead = "4096",
                ReaderQuotaMaxNameTableCharCount = "16384"
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                new[] { endpoint },
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## WCF Endpoints");
            markdown.Should()
                .Contain(
                    $"| SampleLegacyApp.Services.CustomerService |  | basicHttpBinding | CustomerBinding | Transport | Windows | UserName | False | SampleLegacyApp.Contracts.ICustomerService | `{configFilePath}` |");

            markdown.Should().Contain("## WCF Binding Details");
            markdown.Should()
                .Contain(
                    "| SampleLegacyApp.Services.CustomerService | basicHttpBinding | CustomerBinding | 00:01:00 | 00:01:00 | 00:02:00 | 00:10:00 | 1048576 | 65536 | 524288 | Streamed |");

            markdown.Should().Contain("## WCF Reader Quotas");
            markdown.Should()
                .Contain(
                    "| SampleLegacyApp.Services.CustomerService | basicHttpBinding | CustomerBinding | 32 | 8192 | 16384 | 4096 | 16384 |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesWcfServiceContracts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var sourceFilePath = Path.Combine(rootPath, "CustomerContracts.cs");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var contract = new WcfServiceContract
            {
                Name = "ICustomerService",
                SourceFilePath = sourceFilePath,
                Operations =
                {
                    "GetCustomer",
                    "SaveCustomer"
                }
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                new[] { contract },
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## WCF Service Contracts");
            markdown.Should().Contain($"| ICustomerService | GetCustomer, SaveCustomer | `{sourceFilePath}` |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesLegacyAspNetArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var defaultPagePath = Path.Combine(rootPath, "Default.aspx");
            var globalAsaxPath = Path.Combine(rootPath, "Global.asax");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var artifacts = new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.WebFormsPage,
                    Name = "Default.aspx",
                    FilePath = defaultPagePath
                },
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.GlobalAsax,
                    Name = "Global.asax",
                    FilePath = globalAsaxPath
                }
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                artifacts,
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## Legacy ASP.NET Artifacts");
            markdown.Should().Contain("| Kind | Name | File |");
            markdown.Should().Contain($"| WebFormsPage | Default.aspx | `{defaultPagePath}` |");
            markdown.Should().Contain($"| GlobalAsax | Global.asax | `{globalAsaxPath}` |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesNoneRow_WhenNoLegacyAspNetArtifactsExist()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## Legacy ASP.NET Artifacts");
            markdown.Should().Contain("| None | None | None |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_UsesFileName_WhenLegacyAspNetArtifactNameIsEmpty()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var artifactPath = Path.Combine(rootPath, "Default.aspx");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var artifact = new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.WebFormsPage,
                Name = "",
                FilePath = artifactPath
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                new[] { artifact },
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain($"| WebFormsPage | Default.aspx | `{artifactPath}` |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesConfigurationFiles()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var configFilePath = Path.Combine(rootPath, "Web.config");
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var configFile = new DiscoveredConfigFile
            {
                FilePath = configFilePath,
                AppSettingsCount = 12,
                ConnectionStringsCount = 2,
                CustomSectionCount = 1
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                new[] { configFile });

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("## Configuration Files");
            markdown.Should().Contain($"| `{configFilePath}` | 12 | 2 | 1 |");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_IncludesModernisationHints()
    {
        var outputPath = Path.Combine(
            Path.GetTempPath(),
            Guid.NewGuid().ToString("N"),
            "discovery-report.md");

        try
        {
            var writer = new MarkdownReportWriter();

            var hints = new[]
            {
                new ModernisationHint
                {
                    Severity = ModernisationHintSeverity.Risk,
                    Area = "Target Framework",
                    Finding = "LegacyApp targets net48",
                    Reason = ".NET Framework projects usually need extra assessment before migration to modern .NET.",
                    EvidenceKind = "Project",
                    EvidenceName = "LegacyApp",
                    EvidencePath = @"C:\Code\LegacyApp\LegacyApp.csproj",
                    Confidence = ModernisationHintConfidence.High
                }
            };

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<WcfBehaviour>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                hints,
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            Assert.Contains("## Modernisation Hints", markdown);
            Assert.Contains("| Severity | Area | Finding | Evidence | Confidence | Source | Reason |", markdown);
            Assert.Contains("|---|---|---|---|---|---|---|", markdown);
            Assert.Contains("LegacyApp targets net48", markdown);
            Assert.Contains("Project: LegacyApp", markdown);
            Assert.Contains("| High |", markdown);
            Assert.Contains(@"`C:\Code\LegacyApp\LegacyApp.csproj`", markdown);
            Assert.Contains(".NET Framework projects usually need extra assessment before migration to modern .NET.",
                markdown);
        }
        finally
        {
            var directory = Path.GetDirectoryName(outputPath);

            if (!string.IsNullOrWhiteSpace(directory) && Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void Write_EscapesPipeCharactersInMarkdownTables()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");

            var project = new DiscoveredProject
            {
                Name = "Project|WithPipe",
                ProjectFilePath = Path.Combine(rootPath, "Project.csproj"),
                TargetFramework = "net48",
                PackageReferences =
                {
                    "Package|WithPipe"
                },
                AssemblyReferences =
                {
                    "Assembly|WithPipe"
                }
            };

            var hint = new ModernisationHint
            {
                Severity = ModernisationHintSeverity.Warning,
                Area = "Area|WithPipe",
                Finding = "Finding|WithPipe",
                Reason = "Reason|WithPipe"
            };

            var artifact = new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.WebFormsPage,
                Name = "Name|WithPipe.aspx",
                FilePath = Path.Combine(rootPath, "Default.aspx")
            };

            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                new[] { project },
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                new[] { artifact },
                new[] { hint },
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            markdown.Should().Contain("Project\\|WithPipe");
            markdown.Should().Contain("Package\\|WithPipe");
            markdown.Should().Contain("Assembly\\|WithPipe");
            markdown.Should().Contain("Name\\|WithPipe.aspx");
            markdown.Should().Contain("Area\\|WithPipe");
            markdown.Should().Contain("Finding\\|WithPipe");
            markdown.Should().Contain("Reason\\|WithPipe");
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentException_WhenOutputPathIsEmpty()
    {
        var writer = new MarkdownReportWriter();

        var act = () => writer.Write(
            "",
            Array.Empty<DiscoveredSolution>(),
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<ModernisationHint>(),
            Array.Empty<DiscoveredConfigFile>());

        act.Should().Throw<ArgumentException>()
            .WithMessage("Output path cannot be empty.*");
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenSolutionsIsNull()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");
            var writer = new MarkdownReportWriter();

            var act = () => writer.Write(
                outputPath,
                null!,
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            act.Should().Throw<ArgumentNullException>();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenProjectsIsNull()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");
            var writer = new MarkdownReportWriter();

            var act = () => writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                null!,
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            act.Should().Throw<ArgumentNullException>();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenWcfEndpointsIsNull()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");
            var writer = new MarkdownReportWriter();

            var act = () => writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                null!,
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            act.Should().Throw<ArgumentNullException>();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenWcfServiceContractsIsNull()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");
            var writer = new MarkdownReportWriter();

            var act = () => writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                null!,
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            act.Should().Throw<ArgumentNullException>();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenLegacyAspNetArtifactsIsNull()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");
            var writer = new MarkdownReportWriter();

            var act = () => writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                null!,
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            act.Should().Throw<ArgumentNullException>();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenModernisationHintsIsNull()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");
            var writer = new MarkdownReportWriter();

            var act = () => writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                null!,
                Array.Empty<DiscoveredConfigFile>());

            act.Should().Throw<ArgumentNullException>();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_ThrowsArgumentNullException_WhenConfigFilesIsNull()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var outputPath = Path.Combine(rootPath, "discovery-report.md");
            var writer = new MarkdownReportWriter();

            var act = () => writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                null!);

            act.Should().Throw<ArgumentNullException>();
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Write_WhenWcfBehavioursExist_WritesWcfBehaviourSection()
    {
        var outputPath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.md");

        try
        {
            var writer = new MarkdownReportWriter();

            writer.Write(
                outputPath,
                Array.Empty<DiscoveredSolution>(),
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                new[]
                {
                    new WcfBehaviour
                    {
                        Kind = WcfBehaviourKind.ServiceBehaviour,
                        ConfigFilePath = "web.config",
                        Name = "CustomerServiceBehaviour",
                        HasServiceMetadata = true,
                        ServiceMetadataHttpGetEnabled = "true",
                        ServiceMetadataHttpsGetEnabled = "false",
                        HasServiceDebug = true,
                        IncludeExceptionDetailInFaults = "true",
                        HasServiceThrottling = true,
                        MaxConcurrentCalls = "100",
                        MaxConcurrentSessions = "50",
                        MaxConcurrentInstances = "25"
                    },
                    new WcfBehaviour
                    {
                        Kind = WcfBehaviourKind.EndpointBehaviour,
                        ConfigFilePath = "web.config",
                        Name = "JsonEndpointBehaviour",
                        HasWebHttp = true
                    }
                },
                Array.Empty<DiscoveredLegacyAspNetArtifact>(),
                Array.Empty<ModernisationHint>(),
                Array.Empty<DiscoveredConfigFile>());

            var markdown = File.ReadAllText(outputPath);

            Assert.Contains("## WCF Behaviours", markdown);
            Assert.Contains("CustomerServiceBehaviour", markdown);
            Assert.Contains("JsonEndpointBehaviour", markdown);
            Assert.Contains("ServiceBehaviour", markdown);
            Assert.Contains("EndpointBehaviour", markdown);
            Assert.Contains("100", markdown);
            Assert.Contains("True", markdown);
        }
        finally
        {
            if (File.Exists(outputPath))
            {
                File.Delete(outputPath);
            }
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "LegacyLensReportingTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);

        return path;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}