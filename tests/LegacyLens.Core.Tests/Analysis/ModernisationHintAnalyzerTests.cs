using FluentAssertions;
using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerTests
{
    [Fact]
    public void Analyze_ReturnsTargetFrameworkRisk_WhenProjectTargetsNetFramework()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[]
            {
                new DiscoveredProject
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Target Framework" &&
            x.Finding == "SampleLegacyApp.Web targets net48" &&
            x.Reason == ".NET Framework projects usually need extra assessment before migration to modern .NET.");
    }

    [Fact]
    public void Analyze_ReturnsTargetFrameworkWarning_WhenProjectDoesNotDeclareTargetFramework()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "LegacyApp",
                ProjectFilePath = @"C:\Code\LegacyApp\LegacyApp.csproj"
            }
        };

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Target Framework" &&
            x.Finding == "LegacyApp does not declare a target framework" &&
            x.Reason == "Missing target framework information makes migration assessment harder.");
    }

    [Fact]
    public void Analyze_ReturnsProjectDependencyWarning_WhenProjectHasSeveralDirectProjectReferences()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "LegacyApp",
                ProjectFilePath = @"C:\Code\LegacyApp\LegacyApp.csproj",
                ProjectReferences = new List<string>
                {
                    @"..\LegacyApp.Core\LegacyApp.Core.csproj",
                    @"..\LegacyApp.Data\LegacyApp.Data.csproj",
                    @"..\LegacyApp.Services\LegacyApp.Services.csproj"
                }
            }
        };

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Project Dependencies" &&
            x.Finding == "LegacyApp references 3 projects" &&
            x.Reason == "Projects with several direct dependencies may be harder to refactor or migrate independently.");
    }

    [Fact]
    public void Analyze_ReturnsPackageRisk_WhenProjectReferencesSystemServiceModelPackage()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "LegacyApp",
                ProjectFilePath = @"C:\Code\LegacyApp\LegacyApp.csproj",
                PackageReferences = new List<string>
                {
                    "System.ServiceModel.Http"
                }
            }
        };

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Packages" &&
            x.Finding == "LegacyApp references System.ServiceModel.Http" &&
            x.Reason == "System.ServiceModel packages indicate WCF-related usage, which is important for modernisation planning.");
    }

    [Fact]
    public void Analyze_ReturnsPackageWarning_WhenProjectReferencesEntityFramework()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[]
            {
                new DiscoveredProject
                {
                    Name = "SampleLegacyApp.Data",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
                    TargetFramework = "net48",
                    PackageReferences = new List<string>
                    {
                        "EntityFramework"
                    }
                }
            },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Packages" &&
            x.Finding == "SampleLegacyApp.Data references EntityFramework" &&
            x.Reason == "Classic Entity Framework may require assessment before migration to EF Core or modern .NET.");
    }

    [Fact]
    public void Analyze_ReturnsPackageInfo_WhenProjectReferencesNewtonsoftJson()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "LegacyApp",
                ProjectFilePath = @"C:\Code\LegacyApp\LegacyApp.csproj",
                PackageReferences = new List<string>
                {
                    "Newtonsoft.Json"
                }
            }
        };

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Packages" &&
            x.Finding == "LegacyApp references Newtonsoft.Json" &&
            x.Reason == "This is common in legacy and modern projects, but may be reviewed during modernisation.");
    }

    [Fact]
    public void Analyze_ReturnsWcfEndpointRisk_WhenWcfEndpointsExist()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "basicHttpBinding"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF" &&
            x.Finding == "1 WCF endpoint(s) discovered" &&
            x.Reason == "Configured WCF endpoints usually represent service boundaries or integration points that need migration assessment.");
    }

    [Fact]
    public void Analyze_ReturnsWcfServiceContractRisk_WhenWcfServiceContractsExist()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var contracts = new[]
        {
            new WcfServiceContract
            {
                Name = "ICustomerService",
                SourceFilePath = @"C:\Code\LegacyApp.Contracts\ICustomerService.cs"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            contracts,
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF" &&
            x.Finding == "1 WCF service contract(s) discovered" &&
            x.Reason == "WCF service contracts identify service APIs that may need redesign, replacement, or compatibility planning.");
    }

    [Fact]
    public void Analyze_ReturnsBasicHttpBindingWarning_WhenEndpointUsesBasicHttpBinding()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "basicHttpBinding"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Binding" &&
            x.Finding == "basicHttpBinding endpoint discovered for CustomerService" &&
            x.Reason == "basicHttpBinding commonly indicates SOAP interoperability that may need replacement or compatibility planning.");
    }

    [Fact]
    public void Analyze_ReturnsNetTcpBindingRisk_WhenEndpointUsesNetTcpBinding()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[]
            {
                new WcfEndpoint
                {
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Binding = "netTcpBinding",
                    Contract = "SampleLegacyApp.Contracts.ICustomerService"
                }
            },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF Binding" &&
            x.Finding == "netTcpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService" &&
            x.Reason == "netTcpBinding is WCF-specific and usually needs careful migration or replacement planning.");
    }

    [Fact]
    public void Analyze_ReturnsWsHttpBindingWarning_WhenEndpointUsesWsHttpBinding()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "wsHttpBinding"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Binding" &&
            x.Finding == "wsHttpBinding endpoint discovered for CustomerService" &&
            x.Reason == "wsHttpBinding may indicate SOAP and WS-* features that need modernisation assessment.");
    }

    [Fact]
    public void Analyze_ReturnsNetMsmqBindingRisk_WhenEndpointUsesNetMsmqBinding()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "netMsmqBinding"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF Binding" &&
            x.Finding == "netMsmqBinding endpoint discovered for CustomerService" &&
            x.Reason == "netMsmqBinding indicates queue-based WCF integration that needs separate migration planning.");
    }

    [Fact]
    public void Analyze_ReturnsWcfBindingWarning_WhenEndpointHasNoBinding()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[]
            {
                new WcfEndpoint
                {
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Contract = "SampleLegacyApp.Contracts.ICustomerService"
                }
            },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Binding" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has a WCF endpoint without a binding" &&
            x.Reason == "Missing WCF binding information makes endpoint migration assessment harder.");
    }

    [Fact]
    public void Analyze_ReturnsWcfConfigurationInfo_WhenEndpointUsesNamedBindingConfiguration()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[]
            {
                new WcfEndpoint
                {
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Binding = "basicHttpBinding",
                    BindingConfiguration = "CustomerServiceBinding",
                    Contract = "SampleLegacyApp.Contracts.ICustomerService"
                }
            },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Configuration" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses binding configuration CustomerServiceBinding" &&
            x.Reason == "Named WCF binding configurations may contain security, timeout, size, protocol, or credential settings that need migration review.");
    }

    [Fact]
    public void Analyze_ReturnsWcfSecurityWarning_WhenEndpointUsesSecurityMode()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[]
            {
                new WcfEndpoint
                {
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Binding = "basicHttpBinding",
                    Contract = "SampleLegacyApp.Contracts.ICustomerService",
                    SecurityMode = "Transport"
                }
            },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Security" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses WCF security mode Transport" &&
            x.Reason == "WCF security settings need explicit review when replacing WCF endpoints with modern HTTP, JSON, gRPC, or other service endpoints.");
    }

    [Fact]
    public void Analyze_ReturnsWcfSecurityWarning_WhenEndpointUsesTransportCredentialType()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[]
            {
                new WcfEndpoint
                {
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Binding = "basicHttpBinding",
                    Contract = "SampleLegacyApp.Contracts.ICustomerService",
                    TransportClientCredentialType = "Windows"
                }
            },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Security" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses transport credential type Windows" &&
            x.Reason == "Transport credential settings may affect authentication and hosting choices during service migration.");
    }

    [Fact]
    public void Analyze_DoesNotReturnWcfSecurityWarnings_WhenSecurityModeAndTransportCredentialTypeAreNone()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            SecurityMode = "None",
            TransportClientCredentialType = "None",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().NotContain(x =>
            x.Area == "WCF Security" &&
            x.Finding.Contains("security mode", StringComparison.OrdinalIgnoreCase));

        hints.Should().NotContain(x =>
            x.Area == "WCF Security" &&
            x.Finding.Contains("transport credential type", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Analyze_ReturnsWcfMetadataInfo_WhenEndpointIsMetadataExchangeEndpoint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "mexHttpBinding",
                Contract = "IMetadataExchange",
                IsMetadataExchangeEndpoint = true
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Metadata" &&
            x.Finding == "CustomerService exposes a metadata exchange endpoint" &&
            x.Reason == "Metadata exchange endpoints are useful discovery signals when identifying SOAP contracts and generated client dependencies.");
    }

    [Fact]
    public void Analyze_ReturnsWcfTimeoutInfo_WhenEndpointHasTimeoutSettings()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "basicHttpBinding",
                SendTimeout = "00:02:00"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Timeout" &&
            x.Finding == "CustomerService has explicit WCF timeout settings" &&
            x.Reason == "Configured WCF timeout values should be reviewed when replacing endpoints because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ.");
    }

    [Fact]
    public void Analyze_ReturnsWcfBindingLimitsInfo_WhenEndpointHasMessageSizeOrBufferLimits()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "basicHttpBinding",
                MaxReceivedMessageSize = "1048576"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Binding Limits" &&
            x.Finding == "CustomerService has explicit WCF message size or buffer limits" &&
            x.Reason == "Configured WCF message size and buffer limits should be reviewed when migrating endpoints because equivalent request, response, and hosting limits may need to be set explicitly.");
    }

    [Fact]
    public void Analyze_ReturnsWcfTransferModeInfo_WhenEndpointHasNonStreamingTransferMode()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "basicHttpBinding",
                TransferMode = "Buffered"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Transfer Mode" &&
            x.Finding == "CustomerService uses WCF transfer mode Buffered" &&
            x.Reason == "Explicit WCF transfer mode settings should be reviewed when replacing endpoints because modern hosting and client behaviour may differ.");
    }

    [Theory]
    [InlineData("Streamed")]
    [InlineData("StreamedResponse")]
    [InlineData("StreamedRequest")]
    public void Analyze_ReturnsWcfTransferModeWarning_WhenEndpointHasStreamingTransferMode(
        string transferMode)
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "CustomerService",
                ConfigFilePath = @"C:\Code\LegacyApp\web.config",
                Binding = "basicHttpBinding",
                TransferMode = transferMode
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Transfer Mode" &&
            x.Finding == $"CustomerService uses WCF transfer mode {transferMode}" &&
            x.Reason == "Streaming WCF transfer modes may affect endpoint redesign, request buffering, file upload/download behaviour, hosting limits, and client compatibility.");
    }

    [Fact]
    public void Analyze_ReturnsWcfReaderQuotasWarning_WhenEndpointHasReaderQuotaSettings()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[]
            {
                new WcfEndpoint
                {
                    ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    ServiceName = "SampleLegacyApp.Services.CustomerService",
                    Binding = "basicHttpBinding",
                    Contract = "SampleLegacyApp.Contracts.ICustomerService",
                    ReaderQuotaMaxDepth = "32",
                    ReaderQuotaMaxStringContentLength = "8192",
                    ReaderQuotaMaxArrayLength = "16384",
                    ReaderQuotaMaxBytesPerRead = "4096",
                    ReaderQuotaMaxNameTableCharCount = "16384"
                }
            },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Reader Quotas" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has explicit WCF reader quota settings" &&
            x.Reason == "Configured WCF reader quotas may affect XML payload compatibility, maximum object graph depth, string sizes, array sizes, and generated SOAP client behaviour during migration.");
    }

    [Fact]
    public void Analyze_UsesUnknownService_WhenEndpointServiceNameIsMissing()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = null,
            Binding = "basicHttpBinding",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().Contain(x =>
            x.Area == "WCF Binding" &&
            x.Finding == "basicHttpBinding endpoint discovered for Unknown service");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetRisk_WhenProjectReferencesSystemWeb()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "LegacyApp.Web",
                ProjectFilePath = @"C:\Code\LegacyApp.Web\LegacyApp.Web.csproj",
                AssemblyReferences = new List<string>
                {
                    "System.Web"
                }
            }
        };

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "LegacyApp.Web references System.Web" &&
            x.Reason == "System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core.");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenProjectReferencesSystemWebAssembly()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "LegacyApp.Web",
                ProjectFilePath = @"C:\Code\LegacyApp.Web\LegacyApp.Web.csproj",
                AssemblyReferences = new List<string>
                {
                    "System.Web.Mvc"
                }
            }
        };

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "LegacyApp.Web references System.Web.Mvc" &&
            x.Reason == "System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment.");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetRisk_WhenWebFormsPageArtifactExists()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.WebFormsPage,
                Name = "Default.aspx",
                FilePath = @"C:\Code\LegacyApp.Web\Default.aspx"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Default.aspx is a WebForms page" &&
            x.Reason == "WebForms pages indicate classic ASP.NET UI that does not directly migrate to ASP.NET Core and usually needs redesign or replacement planning.");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenWebFormsUserControlArtifactExists()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.WebFormsUserControl,
                Name = "CustomerSummary.ascx",
                FilePath = @"C:\Code\LegacyApp.Web\CustomerSummary.ascx"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "CustomerSummary.ascx is a WebForms user control" &&
            x.Reason == "WebForms user controls may contain reusable UI and page lifecycle behaviour that needs review during ASP.NET Core migration planning.");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenMasterPageArtifactExists()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MasterPage,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Site.Master",
                    Name = "Site.Master"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Site.Master is a WebForms master page" &&
            x.Reason == "Master pages usually indicate shared WebForms layout structure that may need redesign when moving to modern ASP.NET.");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetRisk_WhenAsmxWebServiceArtifactExists()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.AsmxWebService,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Services\CustomerService.asmx",
                    Name = "CustomerService.asmx"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "CustomerService.asmx is an ASMX web service" &&
            x.Reason == "ASMX web services are legacy SOAP-style ASP.NET endpoints that usually need replacement or compatibility planning during modernisation.");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenHttpHandlerArtifactExists()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.HttpHandler,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Handlers\CustomerHandler.ashx",
                    Name = "CustomerHandler.ashx"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "CustomerHandler.ashx is an ASP.NET HTTP handler" &&
            x.Reason == "HTTP handlers may contain custom request processing behaviour that needs mapping to modern ASP.NET middleware, endpoints, or controllers.");
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetInfo_WhenGlobalAsaxArtifactExists()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.GlobalAsax,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Global.asax",
                    Name = "Global.asax"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Global.asax is a Global.asax application file" &&
            x.Reason == "Global.asax may contain application startup, routing, error handling, or lifecycle code that should be reviewed when migrating to modern ASP.NET hosting.");
    }

    [Fact]
    public void Analyze_UsesFileName_WhenLegacyAspNetArtifactNameIsMissing()
    {
        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.WebFormsPage,
            Name = "",
            FilePath = Path.Combine("LegacyApp", "Default.aspx")
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().Contain(x =>
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Default.aspx is a WebForms page");
    }

    [Fact]
    public void Analyze_ReturnsConfigurationWarning_WhenConfigFileHasManyAppSettings()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Code\LegacyApp\app.config",
                AppSettingsCount = 10
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            configFiles);

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Configuration" &&
            x.Finding == "app.config contains 10 appSettings entries" &&
            x.Reason == "A large number of appSettings entries may indicate environment-specific behaviour or operational settings hidden in configuration.");
    }

    [Fact]
    public void Analyze_ReturnsConfigurationInfo_WhenConfigFileHasConnectionStrings()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var configFiles = new[]
        {
            new DiscoveredConfigFile
            {
                FilePath = @"C:\Code\LegacyApp\app.config",
                ConnectionStringsCount = 1
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            configFiles);

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Configuration" &&
            x.Finding == "app.config contains 1 connection string(s)" &&
            x.Reason == "Connection strings identify external data dependencies that should be reviewed during migration planning.");
    }

    [Fact]
    public void Analyze_ReturnsConfigurationWarning_WhenConfigFileHasCustomSections()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<WcfBehaviour>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            new[]
            {
                new DiscoveredConfigFile
                {
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    CustomSectionCount = 2
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Configuration" &&
            x.Finding == "Web.config contains 2 custom configuration section(s)" &&
            x.Reason == "Custom configuration sections may indicate framework-specific or application-specific behaviour that needs migration assessment.");
    }

    [Fact]
    public void Analyze_ReturnsEmptyList_WhenNoReviewSignalsExist()
    {
        var project = new DiscoveredProject
        {
            Name = "ModernApp.Web",
            ProjectFilePath = "ModernApp.Web.csproj",
            TargetFramework = "net8.0"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenProjectsIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var act = () => analyzer.Analyze(
            null!,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenWcfEndpointsIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var act = () => analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            null!,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenWcfServiceContractsIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var act = () => analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            null!,
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_WhenWcfServiceBehaviourHasMetadataDebugAndThrottling_AddsBehaviourHints()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
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
                    HasServiceDebug = true,
                    IncludeExceptionDetailInFaults = "true",
                    HasServiceThrottling = true,
                    MaxConcurrentCalls = "100"
                }
            },
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Behaviour" &&
            x.Finding == "CustomerServiceBehaviour is a WCF service behaviour");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Metadata" &&
            x.Finding == "CustomerServiceBehaviour configures WCF service metadata publishing");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Metadata" &&
            x.Finding == "CustomerServiceBehaviour enables WCF metadata publishing over HTTP or HTTPS");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Debug" &&
            x.Finding == "CustomerServiceBehaviour includes exception detail in WCF faults");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Throttling" &&
            x.Finding == "CustomerServiceBehaviour configures WCF service throttling");
    }

    [Fact]
    public void Analyze_WhenWcfEndpointBehaviourHasWebHttp_AddsRestHint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[]
            {
                new WcfBehaviour
                {
                    Kind = WcfBehaviourKind.EndpointBehaviour,
                    ConfigFilePath = "web.config",
                    Name = "JsonEndpointBehaviour",
                    HasWebHttp = true
                }
            },
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Behaviour" &&
            x.Finding == "JsonEndpointBehaviour is a WCF endpoint behaviour");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF REST" &&
            x.Finding == "JsonEndpointBehaviour uses WCF webHttp endpoint behaviour");
    }
    
    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenLegacyAspNetArtifactsIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var act = () => analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            null!,
            Array.Empty<DiscoveredConfigFile>());

        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenConfigFilesIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var act = () => analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            null!);

        act.Should().Throw<ArgumentNullException>();
    }
    
    [Fact]
    public void Analyze_WhenWcfEndpointHasTimeoutSettings_AddsWcfEndpointEvidenceInsteadOfProjectEvidence()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "SampleLegacyApp.Services",
                ProjectFilePath = @"C:\Code\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj"
            }
        };

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                Binding = "basicHttpBinding",
                SendTimeout = "00:02:00"
            }
        };

        var hints = analyzer.Analyze(
            projects,
            endpoints,
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        var hint = Assert.Single(hints.Where(x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Timeout" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has explicit WCF timeout settings" &&
            x.Reason == "Configured WCF timeout values should be reviewed when replacing endpoints because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ."));

        Assert.Equal("WcfEndpoint", hint.EvidenceKind);
        Assert.Equal("SampleLegacyApp.Services.CustomerService", hint.EvidenceName);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Web\Web.config", hint.EvidencePath);
        Assert.Equal(ModernisationHintConfidence.High, hint.Confidence);
    }
    
    [Fact]
    public void Analyze_WhenWcfServiceBehaviourExists_AddsWcfBehaviourEvidenceInsteadOfEndpointSummaryEvidence()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                Binding = "basicHttpBinding"
            },
            new WcfEndpoint
            {
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                Binding = "mexHttpBinding",
                Contract = "IMetadataExchange",
                IsMetadataExchangeEndpoint = true
            }
        };

        var behaviours = new[]
        {
            new WcfBehaviour
            {
                Kind = WcfBehaviourKind.ServiceBehaviour,
                Name = "CustomerServiceBehaviour",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            behaviours,
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        var hint = Assert.Single(hints.Where(x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Behaviour" &&
            x.Finding == "CustomerServiceBehaviour is a WCF service behaviour" &&
            x.Reason == "WCF service behaviours can contain metadata, debug, throttling, credential, authorization, and runtime settings that need migration review."));

        Assert.Equal("WcfBehaviour", hint.EvidenceKind);
        Assert.Equal("CustomerServiceBehaviour", hint.EvidenceName);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Web\Web.config", hint.EvidencePath);
        Assert.Equal(ModernisationHintConfidence.High, hint.Confidence);
    }
    
    [Fact]
    public void Analyze_WhenWcfEndpointBehaviourExists_AddsWcfBehaviourEvidenceInsteadOfEndpointSummaryEvidence()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                Binding = "basicHttpBinding"
            },
            new WcfEndpoint
            {
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                Binding = "mexHttpBinding",
                Contract = "IMetadataExchange",
                IsMetadataExchangeEndpoint = true
            }
        };

        var behaviours = new[]
        {
            new WcfBehaviour
            {
                Kind = WcfBehaviourKind.EndpointBehaviour,
                Name = "JsonEndpointBehaviour",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>(),
            behaviours,
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        var hint = Assert.Single(hints.Where(x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Behaviour" &&
            x.Finding == "JsonEndpointBehaviour is a WCF endpoint behaviour" &&
            x.Reason == "WCF endpoint behaviours can affect request handling, serialization, dispatch, message inspection, and REST-style endpoint behaviour."));

        Assert.Equal("WcfBehaviour", hint.EvidenceKind);
        Assert.Equal("JsonEndpointBehaviour", hint.EvidenceName);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Web\Web.config", hint.EvidencePath);
        Assert.Equal(ModernisationHintConfidence.High, hint.Confidence);
    }
    
    [Fact]
    public void Analyze_WhenWcfEndpointHasTimeoutSettings_AddsWcfEndpointEvidenceInsteadOfServiceContractEvidence()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new[]
        {
            new WcfEndpoint
            {
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                Binding = "basicHttpBinding",
                SendTimeout = "00:02:00"
            }
        };

        var contracts = new[]
        {
            new WcfServiceContract
            {
                Name = "ICustomerContract",
                SourceFilePath = @"C:\Code\SampleLegacyApp.Contracts\CustomerContracts.cs"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            contracts,
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        var hint = Assert.Single(hints.Where(x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Timeout" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has explicit WCF timeout settings" &&
            x.Reason == "Configured WCF timeout values should be reviewed when replacing endpoints because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ."));

        Assert.Equal("WcfEndpoint", hint.EvidenceKind);
        Assert.Equal("SampleLegacyApp.Services.CustomerService", hint.EvidenceName);
        Assert.Equal(@"C:\Code\SampleLegacyApp.Web\Web.config", hint.EvidencePath);
        Assert.Equal(ModernisationHintConfidence.High, hint.Confidence);
    }
}