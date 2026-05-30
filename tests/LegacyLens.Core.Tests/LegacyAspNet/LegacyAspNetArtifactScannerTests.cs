using LegacyLens.Core.LegacyAspNet;

namespace LegacyLens.Core.Tests.LegacyAspNet;

public sealed class LegacyAspNetArtifactScannerTests
{
    [Fact]
    public void Scan_ReturnsWebFormsPage_WhenAspxFileExists()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Default.aspx");
            File.WriteAllText(filePath, "<%@ Page Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebFormsPage &&
                x.Name == "Default.aspx" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsWebFormsUserControl_WhenAscxFileExists()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "CustomerSummary.ascx");
            File.WriteAllText(filePath, "<%@ Control Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebFormsUserControl &&
                x.Name == "CustomerSummary.ascx" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsMasterPage_WhenMasterFileExists()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Site.master");
            File.WriteAllText(filePath, "<%@ Master Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MasterPage &&
                x.Name == "Site.master" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsAsmxWebService_WhenAsmxFileExists()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "CustomerService.asmx");
            File.WriteAllText(filePath, "<%@ WebService Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.AsmxWebService &&
                x.Name == "CustomerService.asmx" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsHttpHandler_WhenAshxFileExists()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Download.ashx");
            File.WriteAllText(filePath, "<%@ WebHandler Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.HttpHandler &&
                x.Name == "Download.ashx" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsGlobalAsax_WhenGlobalAsaxFileExists()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "Global.asax");
            File.WriteAllText(filePath, "<%@ Application Language=\"C#\" %>");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.GlobalAsax &&
                x.Name == "Global.asax" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsMvcController_WhenClassInheritsFromController()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var controllersPath = Path.Combine(rootPath, "Controllers");
            Directory.CreateDirectory(controllersPath);

            var filePath = Path.Combine(controllersPath, "HomeController.cs");

            File.WriteAllText(
                filePath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Controllers;

                public class HomeController : Controller
                {
                    public ActionResult Index()
                    {
                        return View();
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcController &&
                x.Name == "HomeController" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsMvcController_WhenClassInheritsFromFullyQualifiedMvcController()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var controllersPath = Path.Combine(rootPath, "Controllers");
            Directory.CreateDirectory(controllersPath);

            var filePath = Path.Combine(controllersPath, "CustomersController.cs");

            File.WriteAllText(
                filePath,
                """
                namespace SampleLegacyApp.Web.Controllers;

                public sealed class CustomersController : System.Web.Mvc.Controller
                {
                    public System.Web.Mvc.ActionResult Index()
                    {
                        return View();
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcController &&
                x.Name == "CustomersController" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_DoesNotReturnMvcController_WhenClassNameEndsWithControllerButDoesNotInheritController()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "ReportController.cs");

            File.WriteAllText(
                filePath,
                """
                namespace SampleLegacyApp.Web;

                public class ReportController : SomeOtherBaseClass
                {
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.DoesNotContain(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcController);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsRouteConfig_WhenRouteConfigFileContainsRouteCollection()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var appStartPath = Path.Combine(rootPath, "App_Start");
            Directory.CreateDirectory(appStartPath);

            var filePath = Path.Combine(appStartPath, "RouteConfig.cs");

            File.WriteAllText(
                filePath,
                """
                using System.Web.Mvc;
                using System.Web.Routing;

                namespace SampleLegacyApp.Web;

                public static class RouteConfig
                {
                    public static void RegisterRoutes(RouteCollection routes)
                    {
                        routes.MapRoute(
                            name: "Default",
                            url: "{controller}/{action}/{id}",
                            defaults: new { controller = "Home", action = "Index", id = UrlParameter.Optional });
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.RouteConfig &&
                x.Name == "RouteConfig.cs" &&
                x.FilePath == filePath);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_DoesNotReturnRouteConfig_WhenFileNameMatchesButContentDoesNotLookLikeRoutingConfig()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            var filePath = Path.Combine(rootPath, "RouteConfig.cs");

            File.WriteAllText(
                filePath,
                """
                namespace SampleLegacyApp.Web;

                public static class RouteConfig
                {
                    public static string Name => "Not an ASP.NET route configuration file";
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.DoesNotContain(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.RouteConfig);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    [Fact]
    public void Scan_ReturnsFileBasedAndSourceLevelArtifactsTogether()
    {
        var rootPath = CreateTempDirectory();

        try
        {
            File.WriteAllText(
                Path.Combine(rootPath, "Default.aspx"),
                "<%@ Page Language=\"C#\" %>");

            var controllersPath = Path.Combine(rootPath, "Controllers");
            Directory.CreateDirectory(controllersPath);

            File.WriteAllText(
                Path.Combine(controllersPath, "HomeController.cs"),
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Controllers;

                public class HomeController : Controller
                {
                }
                """);

            var appStartPath = Path.Combine(rootPath, "App_Start");
            Directory.CreateDirectory(appStartPath);

            File.WriteAllText(
                Path.Combine(appStartPath, "RouteConfig.cs"),
                """
                using System.Web.Routing;

                namespace SampleLegacyApp.Web;

                public static class RouteConfig
                {
                    public static void RegisterRoutes(RouteCollection routes)
                    {
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x => x.Kind == LegacyAspNetArtifactKind.WebFormsPage);
            Assert.Contains(artifacts, x => x.Kind == LegacyAspNetArtifactKind.MvcController);
            Assert.Contains(artifacts, x => x.Kind == LegacyAspNetArtifactKind.RouteConfig);
        }
        finally
        {
            DeleteDirectory(rootPath);
        }
    }

    private static string CreateTempDirectory()
    {
        var rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLens.Tests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(rootPath);

        return rootPath;
    }

    private static void DeleteDirectory(string path)
    {
        if (Directory.Exists(path))
        {
            Directory.Delete(path, recursive: true);
        }
    }
}