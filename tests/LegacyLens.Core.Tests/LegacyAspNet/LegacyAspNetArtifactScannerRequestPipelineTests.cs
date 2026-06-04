using LegacyLens.Core.LegacyAspNet;

namespace LegacyLens.Core.Tests.LegacyAspNet;

public sealed class LegacyAspNetArtifactScannerRequestPipelineTests
{
    [Theory]
    [InlineData(
        "DependencyResolver.SetResolver(new UnityDependencyResolver(container));",
        LegacyAspNetArtifactKind.MvcDependencyResolverRegistration,
        "DependencyResolver.SetResolver")]
    [InlineData(
        "ControllerBuilder.Current.SetControllerFactory(new CustomControllerFactory());",
        LegacyAspNetArtifactKind.MvcControllerFactoryRegistration,
        "ControllerBuilder.Current.SetControllerFactory")]
    [InlineData(
        "GlobalFilters.Filters.Add(new HandleErrorAttribute());",
        LegacyAspNetArtifactKind.MvcGlobalFilterRegistration,
        "GlobalFilters.Filters.Add")]
    [InlineData(
        "ModelBinders.Binders.Add(typeof(Customer), new CustomerBinder());",
        LegacyAspNetArtifactKind.MvcModelBinderRegistration,
        "ModelBinders.Binders")]
    [InlineData(
        "ValueProviderFactories.Factories.Add(new JsonValueProviderFactory());",
        LegacyAspNetArtifactKind.MvcValueProviderFactoryRegistration,
        "ValueProviderFactories.Factories")]
    [InlineData(
        "config.DependencyResolver = new UnityResolver(container);",
        LegacyAspNetArtifactKind.WebApiDependencyResolverRegistration,
        "config.DependencyResolver")]
    [InlineData(
        "config.Formatters.JsonFormatter.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();",
        LegacyAspNetArtifactKind.WebApiFormatterConfiguration,
        "config.Formatters")]
    [InlineData(
        "config.MessageHandlers.Add(new CorrelationIdHandler());",
        LegacyAspNetArtifactKind.WebApiMessageHandlerRegistration,
        "config.MessageHandlers.Add")]
    [InlineData(
        "config.Filters.Add(new CustomExceptionFilterAttribute());",
        LegacyAspNetArtifactKind.WebApiFilterRegistration,
        "config.Filters.Add")]
    [InlineData(
        "config.EnableCors();",
        LegacyAspNetArtifactKind.WebApiCorsRegistration,
        "config.EnableCors")]
    public void Scan_WhenSourceContainsRequestPipelineIndicator_AddsArtifact(
        string sourceLine,
        LegacyAspNetArtifactKind expectedKind,
        string expectedName)
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var sourceFilePath = Path.Combine(rootPath, "Startup.cs");

            File.WriteAllText(
                sourceFilePath,
                $$"""
                using System.Web.Http;
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web;

                public static class Startup
                {
                    public static void Configure(HttpConfiguration config)
                    {
                        {{sourceLine}}
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == expectedKind &&
                     x.Name == expectedName &&
                     x.FilePath == sourceFilePath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenSourceContainsGlobalConfigurationDependencyResolver_AddsWebApiDependencyResolverArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var sourceFilePath = Path.Combine(rootPath, "WebApiConfig.cs");

            File.WriteAllText(
                sourceFilePath,
                """
                using System.Web.Http;

                namespace SampleLegacyApp.Web;

                public static class WebApiConfig
                {
                    public static void Register(HttpConfiguration config)
                    {
                        GlobalConfiguration.Configuration.DependencyResolver = new CustomResolver();
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.WebApiDependencyResolverRegistration &&
                     x.Name == "config.DependencyResolver" &&
                     x.FilePath == sourceFilePath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);

        return path;
    }
}