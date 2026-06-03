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
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Web",
            ProjectFilePath = "LegacyApp.Web.csproj",
            TargetFramework = "net48"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "Target Framework",
            Finding = "LegacyApp.Web targets net48",
            Reason = ".NET Framework projects usually need extra assessment before migration to modern .NET."
        });
    }

    [Fact]
    public void Analyze_ReturnsTargetFrameworkWarning_WhenProjectDoesNotDeclareTargetFramework()
    {
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Web",
            ProjectFilePath = "LegacyApp.Web.csproj",
            TargetFramework = null
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Target Framework",
            Finding = "LegacyApp.Web does not declare a target framework",
            Reason = "Missing target framework information makes migration assessment harder."
        });
    }

    [Fact]
    public void Analyze_ReturnsProjectDependencyWarning_WhenProjectHasSeveralDirectProjectReferences()
    {
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Web",
            ProjectFilePath = "LegacyApp.Web.csproj",
            TargetFramework = "net8.0",
            ProjectReferences =
            {
                @"..\LegacyApp.Services\LegacyApp.Services.csproj",
                @"..\LegacyApp.Data\LegacyApp.Data.csproj",
                @"..\LegacyApp.Contracts\LegacyApp.Contracts.csproj"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Project Dependencies",
            Finding = "LegacyApp.Web references 3 projects",
            Reason = "Projects with several direct dependencies may be harder to refactor or migrate independently."
        });
    }

    [Fact]
    public void Analyze_ReturnsPackageRisk_WhenProjectReferencesSystemServiceModelPackage()
    {
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Web",
            ProjectFilePath = "LegacyApp.Web.csproj",
            TargetFramework = "net48",
            PackageReferences =
            {
                "System.ServiceModel.Http"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "Packages",
            Finding = "LegacyApp.Web references System.ServiceModel.Http",
            Reason =
                "System.ServiceModel packages indicate WCF-related usage, which is important for modernisation planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsPackageWarning_WhenProjectReferencesEntityFramework()
    {
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Data",
            ProjectFilePath = "LegacyApp.Data.csproj",
            TargetFramework = "net48",
            PackageReferences =
            {
                "EntityFramework"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Packages",
            Finding = "LegacyApp.Data references EntityFramework",
            Reason = "Classic Entity Framework may require assessment before migration to EF Core or modern .NET."
        });
    }

    [Fact]
    public void Analyze_ReturnsPackageInfo_WhenProjectReferencesNewtonsoftJson()
    {
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Web",
            ProjectFilePath = "LegacyApp.Web.csproj",
            TargetFramework = "net48",
            PackageReferences =
            {
                "Newtonsoft.Json"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "Packages",
            Finding = "LegacyApp.Web references Newtonsoft.Json",
            Reason = "This is common in legacy and modern projects, but may be reviewed during modernisation."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfEndpointRisk_WhenWcfEndpointsExist()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
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

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "WCF",
            Finding = "1 WCF endpoint(s) discovered",
            Reason =
                "Configured WCF endpoints usually represent service boundaries or integration points that need migration assessment."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfServiceContractRisk_WhenWcfServiceContractsExist()
    {
        var contract = new WcfServiceContract
        {
            Name = "ICustomerService",
            SourceFilePath = "CustomerContracts.cs",
            Operations =
            {
                "GetCustomer"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            new[] { contract },
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "WCF",
            Finding = "1 WCF service contract(s) discovered",
            Reason =
                "WCF service contracts identify service APIs that may need redesign, replacement, or compatibility planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsBasicHttpBindingWarning_WhenEndpointUsesBasicHttpBinding()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
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

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "WCF Binding",
            Finding = "basicHttpBinding endpoint discovered for LegacyApp.Services.CustomerService",
            Reason =
                "basicHttpBinding commonly indicates SOAP interoperability that may need replacement or compatibility planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsNetTcpBindingRisk_WhenEndpointUsesNetTcpBinding()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "netTcpBinding",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "WCF Binding",
            Finding = "netTcpBinding endpoint discovered for LegacyApp.Services.CustomerService",
            Reason = "netTcpBinding is WCF-specific and usually needs careful migration or replacement planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsWsHttpBindingWarning_WhenEndpointUsesWsHttpBinding()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "wsHttpBinding",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "WCF Binding",
            Finding = "wsHttpBinding endpoint discovered for LegacyApp.Services.CustomerService",
            Reason = "wsHttpBinding may indicate SOAP and WS-* features that need modernisation assessment."
        });
    }

    [Fact]
    public void Analyze_ReturnsNetMsmqBindingRisk_WhenEndpointUsesNetMsmqBinding()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "netMsmqBinding",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "WCF Binding",
            Finding = "netMsmqBinding endpoint discovered for LegacyApp.Services.CustomerService",
            Reason = "netMsmqBinding indicates queue-based WCF integration that needs separate migration planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfBindingWarning_WhenEndpointHasNoBinding()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = null,
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "WCF Binding",
            Finding = "LegacyApp.Services.CustomerService has a WCF endpoint without a binding",
            Reason = "Missing WCF binding information makes endpoint migration assessment harder."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfConfigurationInfo_WhenEndpointUsesNamedBindingConfiguration()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            BindingConfiguration = "CustomerBinding",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "WCF Configuration",
            Finding = "LegacyApp.Services.CustomerService uses binding configuration CustomerBinding",
            Reason =
                "Named WCF binding configurations may contain security, timeout, size, protocol, or credential settings that need migration review."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfSecurityWarning_WhenEndpointUsesSecurityMode()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            SecurityMode = "Transport",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "WCF Security",
            Finding = "LegacyApp.Services.CustomerService uses WCF security mode Transport",
            Reason =
                "WCF security settings need explicit review when replacing WCF endpoints with modern HTTP, JSON, gRPC, or other service endpoints."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfSecurityWarning_WhenEndpointUsesTransportCredentialType()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            TransportClientCredentialType = "Windows",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "WCF Security",
            Finding = "LegacyApp.Services.CustomerService uses transport credential type Windows",
            Reason =
                "Transport credential settings may affect authentication and hosting choices during service migration."
        });
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
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "mexHttpBinding",
            Contract = "IMetadataExchange",
            IsMetadataExchangeEndpoint = true
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "WCF Metadata",
            Finding = "LegacyApp.Services.CustomerService exposes a metadata exchange endpoint",
            Reason =
                "Metadata exchange endpoints are useful discovery signals when identifying SOAP contracts and generated client dependencies."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfTimeoutInfo_WhenEndpointHasTimeoutSettings()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            OpenTimeout = "00:01:00",
            CloseTimeout = "00:01:00",
            SendTimeout = "00:02:00",
            ReceiveTimeout = "00:10:00",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "WCF Timeout",
            Finding = "LegacyApp.Services.CustomerService has explicit WCF timeout settings",
            Reason =
                "Configured WCF timeout values should be reviewed when replacing endpoints because modern HTTP, JSON, gRPC, hosting, gateway, and client timeout behaviour may differ."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfBindingLimitsInfo_WhenEndpointHasMessageSizeOrBufferLimits()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            MaxReceivedMessageSize = "1048576",
            MaxBufferSize = "65536",
            MaxBufferPoolSize = "524288",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "WCF Binding Limits",
            Finding = "LegacyApp.Services.CustomerService has explicit WCF message size or buffer limits",
            Reason =
                "Configured WCF message size and buffer limits should be reviewed when migrating endpoints because equivalent request, response, and hosting limits may need to be set explicitly."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfTransferModeInfo_WhenEndpointHasNonStreamingTransferMode()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            TransferMode = "Buffered",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "WCF Transfer Mode",
            Finding = "LegacyApp.Services.CustomerService uses WCF transfer mode Buffered",
            Reason =
                "Explicit WCF transfer mode settings should be reviewed when replacing endpoints because modern hosting and client behaviour may differ."
        });
    }

    [Theory]
    [InlineData("Streamed")]
    [InlineData("StreamedRequest")]
    [InlineData("StreamedResponse")]
    public void Analyze_ReturnsWcfTransferModeWarning_WhenEndpointHasStreamingTransferMode(string transferMode)
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            TransferMode = transferMode,
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "WCF Transfer Mode",
            Finding = $"LegacyApp.Services.CustomerService uses WCF transfer mode {transferMode}",
            Reason =
                "Streaming WCF transfer modes may affect endpoint redesign, request buffering, file upload/download behaviour, hosting limits, and client compatibility."
        });
    }

    [Fact]
    public void Analyze_ReturnsWcfReaderQuotasWarning_WhenEndpointHasReaderQuotaSettings()
    {
        var endpoint = new WcfEndpoint
        {
            ConfigFilePath = "Web.config",
            ServiceName = "LegacyApp.Services.CustomerService",
            Binding = "basicHttpBinding",
            ReaderQuotaMaxDepth = "32",
            ReaderQuotaMaxStringContentLength = "8192",
            ReaderQuotaMaxArrayLength = "16384",
            ReaderQuotaMaxBytesPerRead = "4096",
            ReaderQuotaMaxNameTableCharCount = "16384",
            Contract = "LegacyApp.Contracts.ICustomerService"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            new[] { endpoint },
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "WCF Reader Quotas",
            Finding = "LegacyApp.Services.CustomerService has explicit WCF reader quota settings",
            Reason =
                "Configured WCF reader quotas may affect XML payload compatibility, maximum object graph depth, string sizes, array sizes, and generated SOAP client behaviour during migration."
        });
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
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Web",
            ProjectFilePath = "LegacyApp.Web.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.Web"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "Legacy ASP.NET",
            Finding = "LegacyApp.Web references System.Web",
            Reason =
                "System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core."
        });
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenProjectReferencesSystemWebAssembly()
    {
        var project = new DiscoveredProject
        {
            Name = "LegacyApp.Web",
            ProjectFilePath = "LegacyApp.Web.csproj",
            TargetFramework = "net48",
            AssemblyReferences =
            {
                "System.Web.Mvc"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            new[] { project },
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Legacy ASP.NET",
            Finding = "LegacyApp.Web references System.Web.Mvc",
            Reason =
                "System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment."
        });
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetRisk_WhenWebFormsPageArtifactExists()
    {
        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.WebFormsPage,
            Name = "Default.aspx",
            FilePath = @"C:\LegacyApp\Default.aspx"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "Legacy ASP.NET",
            Finding = "Default.aspx is a WebForms page",
            Reason =
                "WebForms pages indicate classic ASP.NET UI that does not directly migrate to ASP.NET Core and usually needs redesign or replacement planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenWebFormsUserControlArtifactExists()
    {
        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.WebFormsUserControl,
            Name = "CustomerSummary.ascx",
            FilePath = @"C:\LegacyApp\CustomerSummary.ascx"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Legacy ASP.NET",
            Finding = "CustomerSummary.ascx is a WebForms user control",
            Reason =
                "WebForms user controls may contain reusable UI and page lifecycle behaviour that needs review during ASP.NET Core migration planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenMasterPageArtifactExists()
    {
        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.MasterPage,
            Name = "Site.master",
            FilePath = @"C:\LegacyApp\Site.master"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Legacy ASP.NET",
            Finding = "Site.master is a WebForms master page",
            Reason =
                "Master pages usually indicate shared WebForms layout structure that may need redesign when moving to modern ASP.NET."
        });
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetRisk_WhenAsmxWebServiceArtifactExists()
    {
        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.AsmxWebService,
            Name = "CustomerService.asmx",
            FilePath = @"C:\LegacyApp\CustomerService.asmx"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Risk,
            Area = "Legacy ASP.NET",
            Finding = "CustomerService.asmx is an ASMX web service",
            Reason =
                "ASMX web services are legacy SOAP-style ASP.NET endpoints that usually need replacement or compatibility planning during modernisation."
        });
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetWarning_WhenHttpHandlerArtifactExists()
    {
        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.HttpHandler,
            Name = "Download.ashx",
            FilePath = @"C:\LegacyApp\Download.ashx"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Legacy ASP.NET",
            Finding = "Download.ashx is an ASP.NET HTTP handler",
            Reason =
                "HTTP handlers may contain custom request processing behaviour that needs mapping to modern ASP.NET middleware, endpoints, or controllers."
        });
    }

    [Fact]
    public void Analyze_ReturnsLegacyAspNetInfo_WhenGlobalAsaxArtifactExists()
    {
        var artifact = new DiscoveredLegacyAspNetArtifact
        {
            Kind = LegacyAspNetArtifactKind.GlobalAsax,
            Name = "Global.asax",
            FilePath = @"C:\LegacyApp\Global.asax"
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[] { artifact },
            Array.Empty<DiscoveredConfigFile>());

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "Legacy ASP.NET",
            Finding = "Global.asax is a Global.asax application file",
            Reason =
                "Global.asax may contain application startup, routing, error handling, or lifecycle code that should be reviewed when migrating to modern ASP.NET hosting."
        });
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
        var configFile = new DiscoveredConfigFile
        {
            FilePath = @"C:\LegacyApp\Web.config",
            AppSettingsCount = 10,
            ConnectionStringsCount = 0,
            CustomSectionCount = 0
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            new[] { configFile });

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Configuration",
            Finding = "Web.config contains 10 appSettings entries",
            Reason =
                "A large number of appSettings entries may indicate environment-specific behaviour or operational settings hidden in configuration."
        });
    }

    [Fact]
    public void Analyze_ReturnsConfigurationInfo_WhenConfigFileHasConnectionStrings()
    {
        var configFile = new DiscoveredConfigFile
        {
            FilePath = @"C:\LegacyApp\Web.config",
            AppSettingsCount = 0,
            ConnectionStringsCount = 2,
            CustomSectionCount = 0
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            new[] { configFile });

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Info,
            Area = "Configuration",
            Finding = "Web.config contains 2 connection string(s)",
            Reason =
                "Connection strings identify external data dependencies that should be reviewed during migration planning."
        });
    }

    [Fact]
    public void Analyze_ReturnsConfigurationWarning_WhenConfigFileHasCustomSections()
    {
        var configFile = new DiscoveredConfigFile
        {
            FilePath = @"C:\LegacyApp\Web.config",
            AppSettingsCount = 0,
            ConnectionStringsCount = 0,
            CustomSectionCount = 1
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            new[] { configFile });

        hints.Should().ContainEquivalentOf(new ModernisationHint
        {
            Severity = ModernisationHintSeverity.Warning,
            Area = "Configuration",
            Finding = "Web.config contains 1 custom configuration section(s)",
            Reason =
                "Custom configuration sections may indicate framework-specific or application-specific behaviour that needs migration assessment."
        });
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
}