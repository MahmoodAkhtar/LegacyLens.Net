using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerLegacyAspNetTests
{
    [Fact]
    public void Analyze_WhenProjectReferencesSystemWeb_AddsLegacyAspNetRiskHint()
    {
        var project = CreateProject(
            "SampleLegacyApp.Web",
            assemblyReferences: new List<string>
            {
                "System.Web"
            });

        var hints = Analyze(projects: new List<DiscoveredProject> { project });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Risk &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "SampleLegacyApp.Web references System.Web" &&
                hint.Reason == "System.Web usually indicates classic ASP.NET, WebForms, MVC 5, ASMX, or ASP.NET-hosted legacy functionality that does not directly migrate to modern ASP.NET Core.");
    }

    [Fact]
    public void Analyze_WhenProjectReferencesSystemWebMvc_AddsLegacyAspNetWarningHint()
    {
        var project = CreateProject(
            "SampleLegacyApp.Web",
            assemblyReferences: new List<string>
            {
                "System.Web.Mvc"
            });

        var hints = Analyze(projects: new List<DiscoveredProject> { project });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Warning &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "SampleLegacyApp.Web references System.Web.Mvc" &&
                hint.Reason == "System.Web-related assemblies indicate legacy ASP.NET functionality that may need separate migration assessment.");
    }

    [Fact]
    public void Analyze_WhenWebFormsPageArtifactExists_AddsLegacyAspNetRiskHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.WebFormsPage,
            "Default.aspx");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Risk &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "Default.aspx is a WebForms page" &&
                hint.Reason == "WebForms pages indicate classic ASP.NET UI that does not directly migrate to ASP.NET Core and usually needs redesign or replacement planning.");
    }

    [Fact]
    public void Analyze_WhenWebFormsUserControlArtifactExists_AddsLegacyAspNetWarningHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.WebFormsUserControl,
            "CustomerSummary.ascx");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Warning &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "CustomerSummary.ascx is a WebForms user control" &&
                hint.Reason == "WebForms user controls may contain reusable UI and page lifecycle behaviour that needs review during ASP.NET Core migration planning.");
    }

    [Fact]
    public void Analyze_WhenMasterPageArtifactExists_AddsLegacyAspNetWarningHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.MasterPage,
            "Site.master");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Warning &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "Site.master is a WebForms master page" &&
                hint.Reason == "Master pages usually indicate shared WebForms layout structure that may need redesign when moving to modern ASP.NET.");
    }

    [Fact]
    public void Analyze_WhenAsmxWebServiceArtifactExists_AddsLegacyAspNetRiskHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.AsmxWebService,
            "CustomerService.asmx");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Risk &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "CustomerService.asmx is an ASMX web service" &&
                hint.Reason == "ASMX web services are legacy SOAP-style ASP.NET endpoints that usually need replacement or compatibility planning during modernisation.");
    }

    [Fact]
    public void Analyze_WhenHttpHandlerArtifactExists_AddsLegacyAspNetWarningHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.HttpHandler,
            "Download.ashx");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Warning &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "Download.ashx is an ASP.NET HTTP handler" &&
                hint.Reason == "HTTP handlers may contain custom request processing behaviour that needs mapping to modern ASP.NET middleware, endpoints, or controllers.");
    }

    [Fact]
    public void Analyze_WhenGlobalAsaxArtifactExists_AddsLegacyAspNetInfoHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.GlobalAsax,
            "Global.asax");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Info &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "Global.asax is a Global.asax application file" &&
                hint.Reason == "Global.asax may contain application startup, routing, error handling, or lifecycle code that should be reviewed when migrating to modern ASP.NET hosting.");
    }

    [Fact]
    public void Analyze_WhenMvcControllerArtifactExists_AddsLegacyAspNetWarningHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.MvcController,
            "HomeController");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Warning &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "HomeController is an ASP.NET MVC controller" &&
                hint.Reason == "ASP.NET MVC controllers may contain routing, action filters, model binding, authentication, or System.Web-specific behaviour that needs review when moving to ASP.NET Core.");
    }

    [Fact]
    public void Analyze_WhenRouteConfigArtifactExists_AddsLegacyAspNetInfoHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.RouteConfig,
            "RouteConfig.cs");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Info &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "RouteConfig.cs is an ASP.NET route configuration file" &&
                hint.Reason == "Route configuration may define URL patterns, defaults, constraints, or ignored routes that should be reviewed when migrating to endpoint routing in ASP.NET Core.");
    }

    [Fact]
    public void Analyze_WhenAreaRegistrationArtifactExists_AddsLegacyAspNetInfoHint()
    {
        var artifact = CreateArtifact(
            LegacyAspNetArtifactKind.AreaRegistration,
            "AdminAreaRegistration");

        var hints = Analyze(legacyAspNetArtifacts: new List<DiscoveredLegacyAspNetArtifact> { artifact });

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Info &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "AdminAreaRegistration is an ASP.NET MVC area registration" &&
                hint.Reason == "ASP.NET MVC area registrations may define area-specific routes and feature boundaries that should be reviewed when migrating to ASP.NET Core endpoint routing.");
    }

    private static IReadOnlyList<ModernisationHint> Analyze(
        IReadOnlyList<DiscoveredProject>? projects = null,
        IReadOnlyList<WcfEndpoint>? wcfEndpoints = null,
        IReadOnlyList<WcfServiceContract>? wcfServiceContracts = null,
        IReadOnlyList<DiscoveredLegacyAspNetArtifact>? legacyAspNetArtifacts = null,
        IReadOnlyList<DiscoveredConfigFile>? configFiles = null)
    {
        var analyzer = new ModernisationHintAnalyzer();

        return analyzer.Analyze(
            projects ?? Array.Empty<DiscoveredProject>(),
            wcfEndpoints ?? Array.Empty<WcfEndpoint>(),
            wcfServiceContracts ?? Array.Empty<WcfServiceContract>(),
            legacyAspNetArtifacts ?? Array.Empty<DiscoveredLegacyAspNetArtifact>(),
            configFiles ?? Array.Empty<DiscoveredConfigFile>());
    }

    private static DiscoveredProject CreateProject(
        string name,
        List<string>? assemblyReferences = null)
    {
        return new DiscoveredProject
        {
            Name = name,
            ProjectFilePath = $@"C:\Code\{name}\{name}.csproj",
            TargetFramework = "net48",
            AssemblyReferences = assemblyReferences ?? new List<string>()
        };
    }

    private static DiscoveredLegacyAspNetArtifact CreateArtifact(
        LegacyAspNetArtifactKind kind,
        string name)
    {
        return new DiscoveredLegacyAspNetArtifact
        {
            Kind = kind,
            Name = name,
            FilePath = $@"C:\Code\SampleLegacyApp.Web\{name}"
        };
    }
}