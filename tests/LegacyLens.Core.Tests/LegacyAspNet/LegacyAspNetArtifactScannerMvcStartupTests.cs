using LegacyLens.Core.LegacyAspNet;

namespace LegacyLens.Core.Tests.LegacyAspNet;

public sealed class LegacyAspNetArtifactScannerMvcStartupTests
{
    [Fact]
    public void Scan_WhenGlobalAsaxCodeBehindHasApplicationStart_AddsMvcStartupRegistrationArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var globalAsaxCodeBehindPath = Path.Combine(rootPath, "Global.asax.cs");

            File.WriteAllText(
                globalAsaxCodeBehindPath,
                """
                using System.Web;
                using System.Web.Mvc;
                using System.Web.Routing;

                namespace SampleLegacyApp.Web;

                public class MvcApplication : HttpApplication
                {
                    protected void Application_Start()
                    {
                        AreaRegistration.RegisterAllAreas();
                        FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
                        RouteConfig.RegisterRoutes(RouteTable.Routes);
                        BundleConfig.RegisterBundles(null);
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcApplicationStartup &&
                     x.Name == "Global.asax.cs Application_Start" &&
                     x.FilePath == globalAsaxCodeBehindPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcAreaRegistrationCall &&
                     x.Name == "AreaRegistration.RegisterAllAreas" &&
                     x.FilePath == globalAsaxCodeBehindPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcFilterRegistrationCall &&
                     x.Name == "FilterConfig.RegisterGlobalFilters" &&
                     x.FilePath == globalAsaxCodeBehindPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcRouteRegistrationCall &&
                     x.Name == "RouteConfig.RegisterRoutes" &&
                     x.FilePath == globalAsaxCodeBehindPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcBundleRegistrationCall &&
                     x.Name == "BundleConfig.RegisterBundles" &&
                     x.FilePath == globalAsaxCodeBehindPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenBundleConfigFileExists_AddsMvcBundleConfigArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var appStartPath = Path.Combine(rootPath, "App_Start");
            Directory.CreateDirectory(appStartPath);

            var bundleConfigPath = Path.Combine(appStartPath, "BundleConfig.cs");

            File.WriteAllText(
                bundleConfigPath,
                """
                namespace SampleLegacyApp.Web;

                public static class BundleConfig
                {
                    public static void RegisterBundles(object bundles)
                    {
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcBundleConfig &&
                     x.Name == "BundleConfig.cs" &&
                     x.FilePath == bundleConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenFilterConfigFileExists_AddsMvcFilterConfigArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var appStartPath = Path.Combine(rootPath, "App_Start");
            Directory.CreateDirectory(appStartPath);

            var filterConfigPath = Path.Combine(appStartPath, "FilterConfig.cs");

            File.WriteAllText(
                filterConfigPath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web;

                public static class FilterConfig
                {
                    public static void RegisterGlobalFilters(GlobalFilterCollection filters)
                    {
                        filters.Add(new HandleErrorAttribute());
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(
                artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcFilterConfig &&
                     x.Name == "FilterConfig.cs" &&
                     x.FilePath == filterConfigPath);
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
            "LegacyLensTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(path);

        return path;
    }
}