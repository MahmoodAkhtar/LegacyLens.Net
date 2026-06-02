using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerLegacyAspNetStartupTests
{
    [Fact]
    public void Analyze_WhenMvcApplicationStartupArtifactExists_AddsStartupInfoHint()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcApplicationStartup,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Global.asax.cs",
                    Name = "Global.asax.cs Application_Start"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Info &&
                 x.Area == "Legacy ASP.NET Startup" &&
                 x.Finding == "Global.asax.cs Application_Start contains ASP.NET application startup code");
    }

    [Fact]
    public void Analyze_WhenMvcStartupRegistrationCallArtifactsExist_AddsRegistrationHints()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcAreaRegistrationCall,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Global.asax.cs",
                    Name = "AreaRegistration.RegisterAllAreas"
                },
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcRouteRegistrationCall,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Global.asax.cs",
                    Name = "RouteConfig.RegisterRoutes"
                },
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcBundleRegistrationCall,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Global.asax.cs",
                    Name = "BundleConfig.RegisterBundles"
                },
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcFilterRegistrationCall,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\Global.asax.cs",
                    Name = "FilterConfig.RegisterGlobalFilters"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Info &&
                 x.Area == "Legacy ASP.NET Startup" &&
                 x.Finding == "AreaRegistration.RegisterAllAreas registers ASP.NET MVC areas");

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Info &&
                 x.Area == "Legacy ASP.NET Routing" &&
                 x.Finding == "RouteConfig.RegisterRoutes registers ASP.NET routes");

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Warning &&
                 x.Area == "Legacy ASP.NET Bundling" &&
                 x.Finding == "BundleConfig.RegisterBundles registers ASP.NET MVC bundles");

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Warning &&
                 x.Area == "Legacy ASP.NET Filters" &&
                 x.Finding == "FilterConfig.RegisterGlobalFilters registers ASP.NET MVC global filters");
    }

    [Fact]
    public void Analyze_WhenMvcBundleAndFilterConfigArtifactsExist_AddsConfigHints()
    {
        var analyzer = new ModernisationHintAnalyzer();

        var hints = analyzer.Analyze(
            Array.Empty<DiscoveredProject>(),
            Array.Empty<WcfEndpoint>(),
            Array.Empty<WcfServiceContract>(),
            new[]
            {
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcBundleConfig,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\App_Start\BundleConfig.cs",
                    Name = "BundleConfig.cs"
                },
                new DiscoveredLegacyAspNetArtifact
                {
                    Kind = LegacyAspNetArtifactKind.MvcFilterConfig,
                    FilePath = @"C:\Code\SampleLegacyApp.Web\App_Start\FilterConfig.cs",
                    Name = "FilterConfig.cs"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Warning &&
                 x.Area == "Legacy ASP.NET Bundling" &&
                 x.Finding == "BundleConfig.cs is an ASP.NET MVC bundle configuration file");

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Warning &&
                 x.Area == "Legacy ASP.NET Filters" &&
                 x.Finding == "FilterConfig.cs is an ASP.NET MVC filter configuration file");
    }
}