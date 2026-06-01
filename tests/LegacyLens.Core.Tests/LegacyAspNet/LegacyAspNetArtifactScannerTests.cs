using LegacyLens.Core.LegacyAspNet;

namespace LegacyLens.Core.Tests.LegacyAspNet;

public sealed class LegacyAspNetArtifactScannerTests : IDisposable
{
    private readonly string _rootPath;

    public LegacyAspNetArtifactScannerTests()
    {
        _rootPath = Path.Combine(
            Path.GetTempPath(),
            "LegacyLensTests",
            Guid.NewGuid().ToString("N"));

        Directory.CreateDirectory(_rootPath);
    }

    [Fact]
    public void Scan_WhenRootPathIsEmpty_ThrowsArgumentException()
    {
        var scanner = new LegacyAspNetArtifactScanner();

        Assert.Throws<ArgumentException>(() => scanner.Scan(""));
    }

    [Fact]
    public void Scan_WhenRootPathDoesNotExist_ThrowsDirectoryNotFoundException()
    {
        var scanner = new LegacyAspNetArtifactScanner();

        Assert.Throws<DirectoryNotFoundException>(() => scanner.Scan(Path.Combine(_rootPath, "missing")));
    }

    [Fact]
    public void Scan_WhenWebFormsPageExists_ReturnsWebFormsPageArtifact()
    {
        var filePath = WriteFile("Default.aspx", "<%@ Page Language=\"C#\" %>");

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.WebFormsPage, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("Default.aspx", artifact.Name);
    }

    [Fact]
    public void Scan_WhenWebFormsUserControlExists_ReturnsWebFormsUserControlArtifact()
    {
        var filePath = WriteFile("CustomerSummary.ascx", "<%@ Control Language=\"C#\" %>");

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.WebFormsUserControl, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("CustomerSummary.ascx", artifact.Name);
    }

    [Fact]
    public void Scan_WhenMasterPageExists_ReturnsMasterPageArtifact()
    {
        var filePath = WriteFile("Site.master", "<%@ Master Language=\"C#\" %>");

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.MasterPage, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("Site.master", artifact.Name);
    }

    [Fact]
    public void Scan_WhenAsmxWebServiceExists_ReturnsAsmxWebServiceArtifact()
    {
        var filePath = WriteFile("CustomerService.asmx", "<%@ WebService Language=\"C#\" %>");

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.AsmxWebService, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("CustomerService.asmx", artifact.Name);
    }

    [Fact]
    public void Scan_WhenHttpHandlerExists_ReturnsHttpHandlerArtifact()
    {
        var filePath = WriteFile("Download.ashx", "<%@ WebHandler Language=\"C#\" %>");

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.HttpHandler, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("Download.ashx", artifact.Name);
    }

    [Fact]
    public void Scan_WhenGlobalAsaxExists_ReturnsGlobalAsaxArtifact()
    {
        var filePath = WriteFile("Global.asax", "<%@ Application Language=\"C#\" %>");

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.GlobalAsax, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("Global.asax", artifact.Name);
    }

    [Fact]
    public void Scan_WhenMvcControllerInheritsFromController_ReturnsMvcControllerArtifact()
    {
        var filePath = WriteFile(
            Path.Combine("Controllers", "HomeController.cs"),
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

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.MvcController, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("HomeController", artifact.Name);
    }

    [Fact]
    public void Scan_WhenMvcControllerInheritsFromFullyQualifiedController_ReturnsMvcControllerArtifact()
    {
        var filePath = WriteFile(
            Path.Combine("Controllers", "HomeController.cs"),
            """
            namespace SampleLegacyApp.Web.Controllers;

            public class HomeController : System.Web.Mvc.Controller
            {
            }
            """);

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.MvcController, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("HomeController", artifact.Name);
    }

    [Fact]
    public void Scan_WhenControllerNameDoesNotInheritFromMvcController_DoesNotReturnMvcControllerArtifact()
    {
        WriteFile(
            Path.Combine("Controllers", "HomeController.cs"),
            """
            namespace SampleLegacyApp.Web.Controllers;

            public class HomeController
            {
            }
            """);

        var artifacts = Scan();

        Assert.Empty(artifacts);
    }

    [Fact]
    public void Scan_WhenRouteConfigFileLooksLikeAspNetRouteConfig_ReturnsRouteConfigArtifact()
    {
        var filePath = WriteFile(
            Path.Combine("App_Start", "RouteConfig.cs"),
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
                        url: "{controller}/{action}/{id}");
                }
            }
            """);

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.RouteConfig, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("RouteConfig.cs", artifact.Name);
    }

    [Fact]
    public void Scan_WhenRouteConfigFileDoesNotLookLikeAspNetRouteConfig_DoesNotReturnRouteConfigArtifact()
    {
        WriteFile(
            Path.Combine("App_Start", "RouteConfig.cs"),
            """
            namespace SampleLegacyApp.Web;

            public static class RouteConfig
            {
                public static string Name => "Not ASP.NET routing";
            }
            """);

        var artifacts = Scan();

        Assert.Empty(artifacts);
    }

    [Fact]
    public void Scan_WhenAreaRegistrationClassExists_ReturnsAreaRegistrationArtifact()
    {
        var filePath = WriteFile(
            Path.Combine("Areas", "Admin", "AdminAreaRegistration.cs"),
            """
            using System.Web.Mvc;

            namespace SampleLegacyApp.Web.Areas.Admin;

            public class AdminAreaRegistration : AreaRegistration
            {
                public override string AreaName => "Admin";

                public override void RegisterArea(AreaRegistrationContext context)
                {
                    context.MapRoute(
                        "Admin_default",
                        "Admin/{controller}/{action}/{id}",
                        new { action = "Index", id = UrlParameter.Optional });
                }
            }
            """);

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.AreaRegistration, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("AdminAreaRegistration", artifact.Name);
    }

    [Fact]
    public void Scan_WhenAreaRegistrationUsesFullyQualifiedBaseType_ReturnsAreaRegistrationArtifact()
    {
        var filePath = WriteFile(
            Path.Combine("Areas", "Admin", "AdminAreaRegistration.cs"),
            """
            namespace SampleLegacyApp.Web.Areas.Admin;

            public class AdminAreaRegistration : System.Web.Mvc.AreaRegistration
            {
                public override string AreaName
                {
                    get
                    {
                        return "Admin";
                    }
                }

                public override void RegisterArea(System.Web.Mvc.AreaRegistrationContext context)
                {
                    context.MapRoute(
                        "Admin_default",
                        "Admin/{controller}/{action}/{id}");
                }
            }
            """);

        var artifacts = Scan();

        var artifact = Assert.Single(artifacts);
        Assert.Equal(LegacyAspNetArtifactKind.AreaRegistration, artifact.Kind);
        Assert.Equal(filePath, artifact.FilePath);
        Assert.Equal("AdminAreaRegistration", artifact.Name);
    }

    [Fact]
    public void Scan_WhenAreaRegistrationClassDoesNotUseMvcAreaRegistration_DoesNotReturnAreaRegistrationArtifact()
    {
        WriteFile(
            Path.Combine("Areas", "Admin", "AdminAreaRegistration.cs"),
            """
            namespace SampleLegacyApp.Web.Areas.Admin;

            public class AdminAreaRegistration
            {
                public string AreaName => "Admin";
            }
            """);

        var artifacts = Scan();

        Assert.Empty(artifacts);
    }

    private IReadOnlyList<DiscoveredLegacyAspNetArtifact> Scan()
    {
        var scanner = new LegacyAspNetArtifactScanner();

        return scanner.Scan(_rootPath);
    }

    private string WriteFile(string relativePath, string content)
    {
        var filePath = Path.Combine(_rootPath, relativePath);
        var directory = Path.GetDirectoryName(filePath);

        if (!string.IsNullOrWhiteSpace(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(filePath, content);

        return filePath;
    }

    public void Dispose()
    {
        if (Directory.Exists(_rootPath))
        {
            Directory.Delete(_rootPath, recursive: true);
        }
    }
}