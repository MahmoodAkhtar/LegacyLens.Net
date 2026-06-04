using LegacyLens.Core.Analysis;
using LegacyLens.Core.Configuration;
using LegacyLens.Core.Discovery;
using LegacyLens.Core.LegacyAspNet;
using LegacyLens.Core.Wcf;

namespace LegacyLens.Core.Tests.Analysis;

public sealed class ModernisationHintAnalyzerRequestPipelineTests
{
    [Theory]
    [InlineData(
        LegacyAspNetArtifactKind.MvcDependencyResolverRegistration,
        "Legacy ASP.NET Dependency Resolution",
        "DependencyResolver.SetResolver configures ASP.NET MVC dependency resolution")]
    [InlineData(
        LegacyAspNetArtifactKind.MvcControllerFactoryRegistration,
        "Legacy ASP.NET Request Pipeline",
        "ControllerBuilder.Current.SetControllerFactory configures an ASP.NET MVC controller factory")]
    [InlineData(
        LegacyAspNetArtifactKind.MvcGlobalFilterRegistration,
        "Legacy ASP.NET Filters",
        "GlobalFilters.Filters.Add registers an ASP.NET MVC global filter")]
    [InlineData(
        LegacyAspNetArtifactKind.MvcModelBinderRegistration,
        "Legacy ASP.NET Model Binding",
        "ModelBinders.Binders configures ASP.NET MVC model binders")]
    [InlineData(
        LegacyAspNetArtifactKind.MvcValueProviderFactoryRegistration,
        "Legacy ASP.NET Model Binding",
        "ValueProviderFactories.Factories configures ASP.NET MVC value provider factories")]
    [InlineData(
        LegacyAspNetArtifactKind.WebApiDependencyResolverRegistration,
        "Legacy ASP.NET Dependency Resolution",
        "config.DependencyResolver configures ASP.NET Web API dependency resolution")]
    [InlineData(
        LegacyAspNetArtifactKind.WebApiFormatterConfiguration,
        "Legacy ASP.NET Web API Pipeline",
        "config.Formatters configures ASP.NET Web API formatters")]
    [InlineData(
        LegacyAspNetArtifactKind.WebApiMessageHandlerRegistration,
        "Legacy ASP.NET Web API Pipeline",
        "config.MessageHandlers.Add registers an ASP.NET Web API message handler")]
    [InlineData(
        LegacyAspNetArtifactKind.WebApiFilterRegistration,
        "Legacy ASP.NET Web API Pipeline",
        "config.Filters.Add registers an ASP.NET Web API filter")]
    [InlineData(
        LegacyAspNetArtifactKind.WebApiCorsRegistration,
        "Legacy ASP.NET Web API Pipeline",
        "config.EnableCors enables ASP.NET Web API CORS configuration")]
    public void Analyze_WhenLegacyAspNetRequestPipelineArtifactExists_AddsWarningHint(
        LegacyAspNetArtifactKind artifactKind,
        string expectedArea,
        string expectedFinding)
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
                    Kind = artifactKind,
                    FilePath = @"C:\Sample\Startup.cs",
                    Name = GetArtifactName(artifactKind)
                }
            },
            Array.Empty<DiscoveredConfigFile>());

        Assert.Contains(
            hints,
            x => x.Severity == ModernisationHintSeverity.Warning &&
                 x.Area == expectedArea &&
                 x.Finding == expectedFinding);
    }

    private static string GetArtifactName(LegacyAspNetArtifactKind artifactKind)
    {
        return artifactKind switch
        {
            LegacyAspNetArtifactKind.MvcDependencyResolverRegistration => "DependencyResolver.SetResolver",
            LegacyAspNetArtifactKind.MvcControllerFactoryRegistration => "ControllerBuilder.Current.SetControllerFactory",
            LegacyAspNetArtifactKind.MvcGlobalFilterRegistration => "GlobalFilters.Filters.Add",
            LegacyAspNetArtifactKind.MvcModelBinderRegistration => "ModelBinders.Binders",
            LegacyAspNetArtifactKind.MvcValueProviderFactoryRegistration => "ValueProviderFactories.Factories",
            LegacyAspNetArtifactKind.WebApiDependencyResolverRegistration => "config.DependencyResolver",
            LegacyAspNetArtifactKind.WebApiFormatterConfiguration => "config.Formatters",
            LegacyAspNetArtifactKind.WebApiMessageHandlerRegistration => "config.MessageHandlers.Add",
            LegacyAspNetArtifactKind.WebApiFilterRegistration => "config.Filters.Add",
            LegacyAspNetArtifactKind.WebApiCorsRegistration => "config.EnableCors",
            _ => throw new ArgumentOutOfRangeException(nameof(artifactKind), artifactKind, null)
        };
    }
}