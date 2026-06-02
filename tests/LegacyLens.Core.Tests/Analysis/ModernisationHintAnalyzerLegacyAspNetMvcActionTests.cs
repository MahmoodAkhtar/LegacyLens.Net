using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerLegacyAspNetMvcActionTests
{
    [Fact]
    public void Analyze_WhenMvcActionArtifactExists_AddsMvcActionInfoHint()
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
                    Kind = LegacyAspNetArtifactKind.MvcAction,
                    FilePath = @"C:\Code\HomeController.cs",
                    Name = "HomeController.Index"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Info &&
                hint.Area == "Legacy ASP.NET" &&
                hint.Finding == "HomeController.Index is an ASP.NET MVC action");
    }

    [Fact]
    public void Analyze_WhenMvcRouteAttributeArtifactExists_AddsMvcRouteAttributeInfoHint()
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
                    Kind = LegacyAspNetArtifactKind.MvcRouteAttribute,
                    FilePath = @"C:\Code\CustomerController.cs",
                    Name = "CustomerController.Details [Route]"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Info &&
                hint.Area == "Legacy ASP.NET Routing" &&
                hint.Finding == "CustomerController.Details [Route] uses ASP.NET MVC attribute routing");
    }

    [Fact]
    public void Analyze_WhenMvcActionAttributeArtifactExists_AddsMvcActionAttributeWarningHint()
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
                    Kind = LegacyAspNetArtifactKind.MvcActionAttribute,
                    FilePath = @"C:\Code\AccountController.cs",
                    Name = "AccountController.Save [ValidateAntiForgeryToken]"
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(
            hints,
            hint =>
                hint.Severity == ModernisationHintSeverity.Warning &&
                hint.Area == "Legacy ASP.NET MVC Attributes" &&
                hint.Finding == "AccountController.Save [ValidateAntiForgeryToken] uses an ASP.NET MVC action attribute");
    }
}