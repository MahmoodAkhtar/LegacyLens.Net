using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerTests
{
    [Fact]
    public void Analyze_ReturnsRiskHint_WhenProjectTargetsNetFramework()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48"
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Target Framework" &&
            x.Finding == "SampleLegacyApp.Web targets net48");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenProjectDoesNotDeclareTargetFramework()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Legacy",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Legacy\SampleLegacyApp.Legacy.csproj"
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Target Framework" &&
            x.Finding == "SampleLegacyApp.Legacy does not declare a target framework");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenProjectHasSeveralDirectProjectReferences()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    ProjectReferences =
                    {
                        @"..\SampleLegacyApp.Services\SampleLegacyApp.Services.csproj",
                        @"..\SampleLegacyApp.Contracts\SampleLegacyApp.Contracts.csproj",
                        @"..\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj"
                    }
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Project Dependencies" &&
            x.Finding == "SampleLegacyApp.Web references 3 projects");
    }

    [Fact]
    public void Analyze_ReturnsRiskHint_WhenProjectReferencesSystemServiceModelPackage()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "System.ServiceModel.Http"
                    }
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Packages" &&
            x.Finding == "SampleLegacyApp.Web references System.ServiceModel.Http");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenProjectReferencesEntityFramework()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Data",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Data\SampleLegacyApp.Data.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "EntityFramework"
                    }
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Packages" &&
            x.Finding == "SampleLegacyApp.Data references EntityFramework");
    }

    [Fact]
    public void Analyze_ReturnsInfoHint_WhenProjectReferencesNewtonsoftJson()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    PackageReferences =
                    {
                        "Newtonsoft.Json"
                    }
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Packages" &&
            x.Finding == "SampleLegacyApp.Web references Newtonsoft.Json");
    }

    [Fact]
    public void Analyze_ReturnsRiskHint_WhenWcfEndpointsAreDiscovered()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint()
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF" &&
            x.Finding == "1 WCF endpoint(s) discovered");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointHasMissingBinding()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(binding: null)
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Binding" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has a WCF endpoint without a binding");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointUsesBasicHttpBinding()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(binding: "basicHttpBinding")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Binding" &&
            x.Finding == "basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService");
    }

    [Fact]
    public void Analyze_ReturnsRiskHint_WhenWcfEndpointUsesNetTcpBinding()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(binding: "netTcpBinding")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF Binding" &&
            x.Finding == "netTcpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointUsesWsHttpBinding()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(binding: "wsHttpBinding")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Binding" &&
            x.Finding == "wsHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService");
    }

    [Fact]
    public void Analyze_ReturnsRiskHint_WhenWcfEndpointUsesNetMsmqBinding()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(binding: "netMsmqBinding")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF Binding" &&
            x.Finding == "netMsmqBinding endpoint discovered for SampleLegacyApp.Services.CustomerService");
    }

    [Fact]
    public void Analyze_ReturnsInfoHint_WhenWcfEndpointUsesNamedBindingConfiguration()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(bindingConfiguration: "CustomerBinding")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Configuration" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses binding configuration CustomerBinding");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointUsesSecurityMode()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(securityMode: "Transport")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Security" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses WCF security mode Transport");
    }

    [Fact]
    public void Analyze_DoesNotReturnSecurityModeHint_WhenWcfEndpointSecurityModeIsNone()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(securityMode: "None")
            });

        Assert.DoesNotContain(hints, x =>
            x.Area == "WCF Security" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses WCF security mode None");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointUsesTransportCredentialType()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(transportClientCredentialType: "Windows")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Security" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses transport credential type Windows");
    }

    [Fact]
    public void Analyze_DoesNotReturnTransportCredentialHint_WhenTransportCredentialTypeIsNone()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(transportClientCredentialType: "None")
            });

        Assert.DoesNotContain(hints, x =>
            x.Area == "WCF Security" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses transport credential type None");
    }

    [Fact]
    public void Analyze_ReturnsInfoHint_WhenWcfEndpointIsMetadataExchangeEndpoint()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(isMetadataExchangeEndpoint: true)
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Metadata" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService exposes a metadata exchange endpoint");
    }

    [Fact]
    public void Analyze_ReturnsInfoHint_WhenWcfEndpointHasTimeouts()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(
                    openTimeout: "00:01:00",
                    closeTimeout: "00:02:00",
                    sendTimeout: "00:03:00",
                    receiveTimeout: "00:10:00")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Timeout" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has explicit WCF timeout settings");
    }

    [Fact]
    public void Analyze_ReturnsInfoHint_WhenWcfEndpointHasMessageSizeOrBufferLimits()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(
                    maxReceivedMessageSize: "1048576",
                    maxBufferSize: "65536",
                    maxBufferPoolSize: "524288")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Binding Limits" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has explicit WCF message size or buffer limits");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointUsesStreamedTransferMode()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(transferMode: "Streamed")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Transfer Mode" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses WCF transfer mode Streamed");
    }

    [Theory]
    [InlineData("Streamed")]
    [InlineData("StreamedRequest")]
    [InlineData("StreamedResponse")]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointUsesStreamingTransferMode(string transferMode)
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(transferMode: transferMode)
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Transfer Mode" &&
            x.Finding == $"SampleLegacyApp.Services.CustomerService uses WCF transfer mode {transferMode}");
    }

    [Fact]
    public void Analyze_ReturnsInfoHint_WhenWcfEndpointUsesBufferedTransferMode()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(transferMode: "Buffered")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "WCF Transfer Mode" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService uses WCF transfer mode Buffered");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenWcfEndpointHasReaderQuotas()
    {
        var hints = Analyze(
            wcfEndpoints: new List<WcfEndpoint>
            {
                CreateEndpoint(
                    readerQuotaMaxDepth: "32",
                    readerQuotaMaxStringContentLength: "8192",
                    readerQuotaMaxArrayLength: "16384",
                    readerQuotaMaxBytesPerRead: "4096",
                    readerQuotaMaxNameTableCharCount: "16384")
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "WCF Reader Quotas" &&
            x.Finding == "SampleLegacyApp.Services.CustomerService has explicit WCF reader quota settings");
    }

    [Fact]
    public void Analyze_ReturnsRiskHint_WhenWcfServiceContractsAreDiscovered()
    {
        var hints = Analyze(
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

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "WCF" &&
            x.Finding == "1 WCF service contract(s) discovered");
    }

    [Fact]
    public void Analyze_ReturnsRiskHint_WhenProjectReferencesSystemWeb()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    AssemblyReferences =
                    {
                        "System.Web"
                    }
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "SampleLegacyApp.Web references System.Web");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenProjectReferencesSystemWebRelatedAssembly()
    {
        var hints = Analyze(
            projects: new List<DiscoveredProject>
            {
                new()
                {
                    Name = "SampleLegacyApp.Web",
                    ProjectFilePath = @"C:\Code\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                    TargetFramework = "net48",
                    AssemblyReferences =
                    {
                        "System.Web.Mvc"
                    }
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "SampleLegacyApp.Web references System.Web.Mvc");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenConfigFileHasManyAppSettings()
    {
        var hints = Analyze(
            configFiles: new List<DiscoveredConfigFile>
            {
                new()
                {
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    AppSettingsCount = 10,
                    ConnectionStringsCount = 0,
                    CustomSectionCount = 0
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Configuration" &&
            x.Finding == "Web.config contains 10 appSettings entries");
    }

    [Fact]
    public void Analyze_ReturnsInfoHint_WhenConfigFileHasConnectionStrings()
    {
        var hints = Analyze(
            configFiles: new List<DiscoveredConfigFile>
            {
                new()
                {
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    AppSettingsCount = 0,
                    ConnectionStringsCount = 2,
                    CustomSectionCount = 0
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Configuration" &&
            x.Finding == "Web.config contains 2 connection string(s)");
    }

    [Fact]
    public void Analyze_ReturnsWarningHint_WhenConfigFileHasCustomSections()
    {
        var hints = Analyze(
            configFiles: new List<DiscoveredConfigFile>
            {
                new()
                {
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
                    AppSettingsCount = 0,
                    ConnectionStringsCount = 0,
                    CustomSectionCount = 1
                }
            });

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Configuration" &&
            x.Finding == "Web.config contains 1 custom configuration section(s)");
    }

    [Fact]
    public void Analyze_ReturnsEmptyList_WhenNoInputsContainReviewSignals()
    {
        var hints = Analyze();

        Assert.Empty(hints);
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenProjectsIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                null!,
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenWcfEndpointsIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                null!,
                Array.Empty<WcfServiceContract>(),
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenWcfServiceContractsIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                null!,
                Array.Empty<DiscoveredConfigFile>()));
    }

    [Fact]
    public void Analyze_ThrowsArgumentNullException_WhenConfigFilesIsNull()
    {
        var analyzer = new ModernisationHintAnalyzer();

        Assert.Throws<ArgumentNullException>(() =>
            analyzer.Analyze(
                Array.Empty<DiscoveredProject>(),
                Array.Empty<WcfEndpoint>(),
                Array.Empty<WcfServiceContract>(),
                null!));
    }

    private static IReadOnlyList<ModernisationHint> Analyze(
        IReadOnlyList<DiscoveredProject>? projects = null,
        IReadOnlyList<WcfEndpoint>? wcfEndpoints = null,
        IReadOnlyList<WcfServiceContract>? wcfServiceContracts = null,
        IReadOnlyList<DiscoveredConfigFile>? configFiles = null)
    {
        var analyzer = new ModernisationHintAnalyzer();

        return analyzer.Analyze(
            projects ?? Array.Empty<DiscoveredProject>(),
            wcfEndpoints ?? Array.Empty<WcfEndpoint>(),
            wcfServiceContracts ?? Array.Empty<WcfServiceContract>(),
            configFiles ?? Array.Empty<DiscoveredConfigFile>());
    }

    private static WcfEndpoint CreateEndpoint(
        string? serviceName = "SampleLegacyApp.Services.CustomerService",
        string? binding = "basicHttpBinding",
        string? bindingConfiguration = null,
        string? securityMode = null,
        string? transportClientCredentialType = null,
        bool isMetadataExchangeEndpoint = false,
        string? openTimeout = null,
        string? closeTimeout = null,
        string? sendTimeout = null,
        string? receiveTimeout = null,
        string? maxReceivedMessageSize = null,
        string? maxBufferSize = null,
        string? maxBufferPoolSize = null,
        string? transferMode = null,
        string? readerQuotaMaxDepth = null,
        string? readerQuotaMaxStringContentLength = null,
        string? readerQuotaMaxArrayLength = null,
        string? readerQuotaMaxBytesPerRead = null,
        string? readerQuotaMaxNameTableCharCount = null)
    {
        return new WcfEndpoint
        {
            ConfigFilePath = @"C:\Code\SampleLegacyApp.Web\Web.config",
            ServiceName = serviceName,
            Address = "",
            Binding = binding,
            BindingConfiguration = bindingConfiguration,
            SecurityMode = securityMode,
            TransportClientCredentialType = transportClientCredentialType,
            IsMetadataExchangeEndpoint = isMetadataExchangeEndpoint,
            Contract = "SampleLegacyApp.Contracts.ICustomerService",
            OpenTimeout = openTimeout,
            CloseTimeout = closeTimeout,
            SendTimeout = sendTimeout,
            ReceiveTimeout = receiveTimeout,
            MaxReceivedMessageSize = maxReceivedMessageSize,
            MaxBufferSize = maxBufferSize,
            MaxBufferPoolSize = maxBufferPoolSize,
            TransferMode = transferMode,
            ReaderQuotaMaxDepth = readerQuotaMaxDepth,
            ReaderQuotaMaxStringContentLength = readerQuotaMaxStringContentLength,
            ReaderQuotaMaxArrayLength = readerQuotaMaxArrayLength,
            ReaderQuotaMaxBytesPerRead = readerQuotaMaxBytesPerRead,
            ReaderQuotaMaxNameTableCharCount = readerQuotaMaxNameTableCharCount
        };
    }
}