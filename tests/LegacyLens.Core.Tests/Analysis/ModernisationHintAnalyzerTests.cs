using LegacyLens.Core.Analysis;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerTests
{
    [Fact]
    public void Analyze_WhenProjectTargetsNetFramework_AddsRiskHint()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Legacy.Web",
                ProjectFilePath = @"C:\Code\Legacy.Web\Legacy.Web.csproj",
                TargetFramework = "net48"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Risk &&
            hint.Area == "Target Framework" &&
            hint.Finding.Contains("Legacy.Web") &&
            hint.Finding.Contains("net48"));
    }

    [Fact]
    public void Analyze_WhenProjectDoesNotDeclareTargetFramework_AddsWarningHint()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Unknown.Project",
                ProjectFilePath = @"C:\Code\Unknown.Project\Unknown.Project.csproj",
                TargetFramework = null
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Warning &&
            hint.Area == "Target Framework" &&
            hint.Finding.Contains("Unknown.Project") &&
            hint.Finding.Contains("does not declare a target framework"));
    }

    [Fact]
    public void Analyze_WhenProjectHasThreeProjectReferences_AddsProjectCouplingWarningHint()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Legacy.Web",
                ProjectFilePath = @"C:\Code\Legacy.Web\Legacy.Web.csproj",
                TargetFramework = "net48",
                ProjectReferences = new List<string>
                {
                    @"..\Legacy.Services\Legacy.Services.csproj",
                    @"..\Legacy.Data\Legacy.Data.csproj",
                    @"..\Legacy.Contracts\Legacy.Contracts.csproj"
                }
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Warning &&
            hint.Area == "Project Dependencies" &&
            hint.Finding.Contains("Legacy.Web") &&
            hint.Finding.Contains("references 3 projects"));
    }

    [Fact]
    public void Analyze_WhenProjectReferencesSystemServiceModelPackage_AddsPackageRiskHint()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Legacy.Web",
                ProjectFilePath = @"C:\Code\Legacy.Web\Legacy.Web.csproj",
                TargetFramework = "net48",
                PackageReferences = new List<string>
                {
                    "System.ServiceModel.Http"
                }
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Risk &&
            hint.Area == "Packages" &&
            hint.Finding.Contains("Legacy.Web") &&
            hint.Finding.Contains("System.ServiceModel.Http"));
    }

    [Fact]
    public void Analyze_WhenProjectReferencesEntityFramework_AddsPackageWarningHint()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Legacy.Data",
                ProjectFilePath = @"C:\Code\Legacy.Data\Legacy.Data.csproj",
                TargetFramework = "net48",
                PackageReferences = new List<string>
                {
                    "EntityFramework"
                }
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Warning &&
            hint.Area == "Packages" &&
            hint.Finding.Contains("Legacy.Data") &&
            hint.Finding.Contains("EntityFramework"));
    }

    [Fact]
    public void Analyze_WhenProjectReferencesNewtonsoftJson_AddsPackageInfoHint()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Legacy.Web",
                ProjectFilePath = @"C:\Code\Legacy.Web\Legacy.Web.csproj",
                TargetFramework = "net48",
                PackageReferences = new List<string>
                {
                    "Newtonsoft.Json"
                }
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Info &&
            hint.Area == "Packages" &&
            hint.Finding.Contains("Legacy.Web") &&
            hint.Finding.Contains("Newtonsoft.Json"));
    }

    [Fact]
    public void Analyze_WhenWcfEndpointExists_AddsWcfEndpointRiskHint()
    {
        var endpoints = new List<WcfEndpoint>
        {
            new()
            {
                ConfigFilePath = @"C:\Code\Legacy.Web\Web.config",
                ServiceName = "Legacy.Services.CustomerService",
                Address = "",
                Binding = "basicHttpBinding",
                Contract = "Legacy.Contracts.ICustomerService"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Risk &&
            hint.Area == "WCF" &&
            hint.Finding.Contains("1 WCF endpoint"));
    }

    [Fact]
    public void Analyze_WhenWcfServiceContractExists_AddsWcfServiceContractRiskHint()
    {
        var contracts = new List<WcfServiceContract>
        {
            new()
            {
                Name = "ICustomerService",
                SourceFilePath = @"C:\Code\Legacy.Contracts\CustomerContracts.cs",
                Operations = new List<string>
                {
                    "GetCustomer"
                }
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            contracts);

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Risk &&
            hint.Area == "WCF" &&
            hint.Finding.Contains("1 WCF service contract"));
    }

    [Fact]
    public void Analyze_WhenNoRisksWarningsOrInfoAreFound_ReturnsEmptyHintList()
    {
        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "Modern.Api",
                ProjectFilePath = @"C:\Code\Modern.Api\Modern.Api.csproj",
                TargetFramework = "net8.0"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>());

        Assert.Empty(hints);
    }

    [Fact]
    public void Analyze_AddsWarningHint_ForBasicHttpBinding()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new List<WcfEndpoint>
        {
            new()
            {
                ConfigFilePath = "web.config",
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                Address = "",
                Binding = "basicHttpBinding",
                Contract = "SampleLegacyApp.Contracts.ICustomerService"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Warning &&
            hint.Area == "WCF Binding" &&
            hint.Finding == "basicHttpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService");
    }

    [Fact]
    public void Analyze_AddsRiskHint_ForNetTcpBinding()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new List<WcfEndpoint>
        {
            new()
            {
                ConfigFilePath = "app.config",
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                Address = "net.tcp://localhost/CustomerService",
                Binding = "netTcpBinding",
                Contract = "SampleLegacyApp.Contracts.ICustomerService"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Risk &&
            hint.Area == "WCF Binding" &&
            hint.Finding == "netTcpBinding endpoint discovered for SampleLegacyApp.Services.CustomerService");
    }

    [Fact]
    public void Analyze_AddsWarningHint_WhenWcfEndpointBindingIsMissing()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var endpoints = new List<WcfEndpoint>
        {
            new()
            {
                ConfigFilePath = "web.config",
                ServiceName = "SampleLegacyApp.Services.CustomerService",
                Address = "",
                Binding = null,
                Contract = "SampleLegacyApp.Contracts.ICustomerService"
            }
        };

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            endpoints,
            Array.Empty<WcfServiceContract>());

        Assert.Contains(hints, hint =>
            hint.Severity == ModernisationHintSeverity.Warning &&
            hint.Area == "WCF Binding" &&
            hint.Finding == "SampleLegacyApp.Services.CustomerService has a WCF endpoint without a binding");
    }
}