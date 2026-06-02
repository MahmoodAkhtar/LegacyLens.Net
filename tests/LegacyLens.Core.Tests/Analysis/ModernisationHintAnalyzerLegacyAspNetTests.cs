using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerLegacyAspNetTests
{
    [Fact]
    public void Analyze_WhenProjectReferencesSystemWeb_AddsRiskHint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = @"C:\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                AssemblyReferences = new List<string>
                {
                    "System.Web"
                }
            }
        };

        var hints = analyzer.Analyze(
            projects,
            new List<WcfEndpoint>(),
            new List<WcfServiceContract>(),
            new List<DiscoveredLegacyAspNetArtifact>(),
            new List<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "SampleLegacyApp.Web references System.Web");
    }

    [Fact]
    public void Analyze_WhenProjectReferencesSystemWebMvc_AddsWarningHint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var projects = new List<DiscoveredProject>
        {
            new()
            {
                Name = "SampleLegacyApp.Web",
                ProjectFilePath = @"C:\SampleLegacyApp.Web\SampleLegacyApp.Web.csproj",
                AssemblyReferences = new List<string>
                {
                    "System.Web.Mvc"
                }
            }
        };

        var hints = analyzer.Analyze(
            projects,
            new List<WcfEndpoint>(),
            new List<WcfServiceContract>(),
            new List<DiscoveredLegacyAspNetArtifact>(),
            new List<DiscoveredConfigFile>());

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "SampleLegacyApp.Web references System.Web.Mvc");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebFormsPage_AddsRiskHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebFormsPage, "Default.aspx");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Default.aspx is a WebForms page");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebFormsUserControl_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebFormsUserControl, "CustomerSummary.ascx");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "CustomerSummary.ascx is a WebForms user control");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMasterPage_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MasterPage, "Site.master");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Site.master is a WebForms master page");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsAsmxWebService_AddsRiskHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.AsmxWebService, "CustomerService.asmx");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Risk &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "CustomerService.asmx is an ASMX web service");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsHttpHandler_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.HttpHandler, "Download.ashx");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Download.ashx is an ASP.NET HTTP handler");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsGlobalAsax_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.GlobalAsax, "Global.asax");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "Global.asax is a Global.asax application file");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcController_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcController, "HomeController");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "HomeController is an ASP.NET MVC controller");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcAction_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcAction, "HomeController.Index");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "HomeController.Index is an ASP.NET MVC action");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcRouteAttribute_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcRouteAttribute, "HomeController.Index [Route]");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Routing" &&
            x.Finding == "HomeController.Index [Route] uses ASP.NET MVC attribute routing");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcActionAttribute_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcActionAttribute, "HomeController.Save [HttpPost]");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET MVC Attributes" &&
            x.Finding == "HomeController.Save [HttpPost] uses an ASP.NET MVC action attribute");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsRouteConfig_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.RouteConfig, "RouteConfig.cs");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "RouteConfig.cs is an ASP.NET route configuration file");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsAreaRegistration_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.AreaRegistration, "AdminAreaRegistration");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET" &&
            x.Finding == "AdminAreaRegistration is an ASP.NET MVC area registration");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcApplicationStartup_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcApplicationStartup, "Global.asax.cs Application_Start");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Startup" &&
            x.Finding == "Global.asax.cs Application_Start contains ASP.NET application startup code");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcAreaRegistrationCall_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcAreaRegistrationCall, "AreaRegistration.RegisterAllAreas");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Startup" &&
            x.Finding == "AreaRegistration.RegisterAllAreas registers ASP.NET MVC areas");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcRouteRegistrationCall_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcRouteRegistrationCall, "RouteConfig.RegisterRoutes");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Routing" &&
            x.Finding == "RouteConfig.RegisterRoutes registers ASP.NET routes");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcBundleConfig_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcBundleConfig, "BundleConfig.cs");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET Bundling" &&
            x.Finding == "BundleConfig.cs is an ASP.NET MVC bundle configuration file");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcBundleRegistrationCall_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcBundleRegistrationCall, "BundleConfig.RegisterBundles");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET Bundling" &&
            x.Finding == "BundleConfig.RegisterBundles registers ASP.NET MVC bundles");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcFilterConfig_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcFilterConfig, "FilterConfig.cs");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET Filters" &&
            x.Finding == "FilterConfig.cs is an ASP.NET MVC filter configuration file");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsMvcFilterRegistrationCall_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.MvcFilterRegistrationCall, "FilterConfig.RegisterGlobalFilters");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET Filters" &&
            x.Finding == "FilterConfig.RegisterGlobalFilters registers ASP.NET MVC global filters");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebApiController_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebApiController, "CustomersApiController");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET Web API" &&
            x.Finding == "CustomersApiController is an ASP.NET Web API controller");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebApiAction_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebApiAction, "CustomersApiController.Get");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Web API" &&
            x.Finding == "CustomersApiController.Get is an ASP.NET Web API action");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebApiRouteAttribute_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebApiRouteAttribute, "CustomersApiController.Get [Route]");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Web API Routing" &&
            x.Finding == "CustomersApiController.Get [Route] uses ASP.NET Web API attribute routing");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebApiActionAttribute_AddsWarningHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebApiActionAttribute, "CustomersApiController.Get [HttpGet]");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Warning &&
            x.Area == "Legacy ASP.NET Web API Attributes" &&
            x.Finding == "CustomersApiController.Get [HttpGet] uses an ASP.NET Web API action attribute");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebApiConfig_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebApiConfig, "WebApiConfig.cs");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Web API" &&
            x.Finding == "WebApiConfig.cs is an ASP.NET Web API configuration file");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebApiRouteRegistrationCall_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebApiRouteRegistrationCall, "MapHttpRoute");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Web API Routing" &&
            x.Finding == "MapHttpRoute registers ASP.NET Web API routes");
    }

    [Fact]
    public void Analyze_WhenLegacyAspNetArtifactIsWebApiGlobalConfigurationCall_AddsInfoHint()
    {
        var hints = AnalyzeArtifact(LegacyAspNetArtifactKind.WebApiGlobalConfigurationCall, "GlobalConfiguration.Configure");

        Assert.Contains(hints, x =>
            x.Severity == ModernisationHintSeverity.Info &&
            x.Area == "Legacy ASP.NET Web API Startup" &&
            x.Finding == "GlobalConfiguration.Configure registers ASP.NET Web API startup configuration");
    }

    private static IReadOnlyList<ModernisationHint> AnalyzeArtifact(
        LegacyAspNetArtifactKind kind,
        string name)
    {
        var analyzer = new ModernisationHintAnalyzer();

        return analyzer.Analyze(
            new List<DiscoveredProject>(),
            new List<WcfEndpoint>(),
            new List<WcfServiceContract>(),
            new List<DiscoveredLegacyAspNetArtifact>
            {
                new()
                {
                    Kind = kind,
                    Name = name,
                    FilePath = $@"C:\SampleLegacyApp.Web\{name}"
                }
            },
            new List<DiscoveredConfigFile>());
    }
}