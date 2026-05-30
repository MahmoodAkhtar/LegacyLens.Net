using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerLegacyAspNetTests
{
    [Fact]
    public void Analyze_WhenSystemWebAssemblyReferenceExists_AddsLegacyAspNetRiskHint()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = @"C:\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                AssemblyReferences = new List<string>
                {
                    "System.Web"
                }
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "SampleLegacyApp.Web references System.Web");
    }

    [Fact]
    public void Analyze_WhenSystemWebMvcAssemblyReferenceExists_AddsLegacyAspNetWarningHint()
    {
        var projects = new[]
        {
            new DiscoveredProject
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = @"C:\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                AssemblyReferences = new List<string>
                {
                    "System.Web.Mvc"
                }
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            projects,
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "SampleLegacyApp.Web references System.Web.Mvc");
    }

    [Fact]
    public void Analyze_WhenWebFormsPageDiscovered_AddsLegacyAspNetRiskHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.WebFormsPage,
                FilePath = @"C:\SampleLegacyApp.Web\Default.aspx",
                Name = "Default.aspx"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Default.aspx is a WebForms page");
    }

    [Fact]
    public void Analyze_WhenWebFormsUserControlDiscovered_AddsLegacyAspNetWarningHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.WebFormsUserControl,
                FilePath = @"C:\SampleLegacyApp.Web\CustomerSummary.ascx",
                Name = "CustomerSummary.ascx"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "CustomerSummary.ascx is a WebForms user control");
    }

    [Fact]
    public void Analyze_WhenMasterPageDiscovered_AddsLegacyAspNetWarningHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.MasterPage,
                FilePath = @"C:\SampleLegacyApp.Web\Site.master",
                Name = "Site.master"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Site.master is a WebForms master page");
    }

    [Fact]
    public void Analyze_WhenAsmxWebServiceDiscovered_AddsLegacyAspNetRiskHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.AsmxWebService,
                FilePath = @"C:\SampleLegacyApp.Web\CustomerService.asmx",
                Name = "CustomerService.asmx"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "CustomerService.asmx is an ASMX web service");
    }

    [Fact]
    public void Analyze_WhenHttpHandlerDiscovered_AddsLegacyAspNetWarningHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.HttpHandler,
                FilePath = @"C:\SampleLegacyApp.Web\Download.ashx",
                Name = "Download.ashx"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Download.ashx is an ASP.NET HTTP handler");
    }

    [Fact]
    public void Analyze_WhenGlobalAsaxDiscovered_AddsLegacyAspNetInfoHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.GlobalAsax,
                FilePath = @"C:\SampleLegacyApp.Web\Global.asax",
                Name = "Global.asax"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Global.asax is a Global.asax application file");
    }

    [Fact]
    public void Analyze_WhenMvcControllerDiscovered_AddsLegacyAspNetWarningHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.MvcController,
                FilePath = @"C:\SampleLegacyApp.Web\Controllers\HomeController.cs",
                Name = "HomeController"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "HomeController is an ASP.NET MVC controller");
    }

    [Fact]
    public void Analyze_WhenRouteConfigDiscovered_AddsLegacyAspNetInfoHint()
    {
        var artifacts = new[]
        {
            new DiscoveredLegacyAspNetArtifact
            {
                Kind = LegacyAspNetArtifactKind.RouteConfig,
                FilePath = @"C:\SampleLegacyApp.Web\App_Start\RouteConfig.cs",
                Name = "RouteConfig.cs"
            }
        };

        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            artifacts,
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "RouteConfig.cs is an ASP.NET route configuration file");
    }
}