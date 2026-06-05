using FluentAssertions;
using LegacyLens.Core.LegacyAspNet;

namespace LegacyLens.Core.Tests.LegacyAspNet;

public sealed class LegacyAspNetArtifactScannerTests
{
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

        Assert.Throws<DirectoryNotFoundException>(() => scanner.Scan(@"C:\path-that-does-not-exist"));
    }

    [Fact]
    public void Scan_WhenClassicAspNetFilesExist_AddsFileBasedArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            File.WriteAllText(Path.Combine(rootPath, "Default.aspx"), "");
            File.WriteAllText(Path.Combine(rootPath, "CustomerSummary.ascx"), "");
            File.WriteAllText(Path.Combine(rootPath, "Site.master"), "");
            File.WriteAllText(Path.Combine(rootPath, "CustomerService.asmx"), "");
            File.WriteAllText(Path.Combine(rootPath, "Download.ashx"), "");
            File.WriteAllText(Path.Combine(rootPath, "Global.asax"), "");

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.WebFormsPage && x.Name == "Default.aspx");
            Assert.Contains(artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.WebFormsUserControl && x.Name == "CustomerSummary.ascx");
            Assert.Contains(artifacts, x => x.Kind == LegacyAspNetArtifactKind.MasterPage && x.Name == "Site.master");
            Assert.Contains(artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.AsmxWebService && x.Name == "CustomerService.asmx");
            Assert.Contains(artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.HttpHandler && x.Name == "Download.ashx");
            Assert.Contains(artifacts, x => x.Kind == LegacyAspNetArtifactKind.GlobalAsax && x.Name == "Global.asax");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenMvcControllerExists_AddsMvcControllerArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "HomeController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Controllers;

                public class HomeController : Controller
                {
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcController &&
                x.Name == "HomeController" &&
                x.FilePath == controllerPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenMvcControllerHasActions_AddsMvcActionArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "HomeController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Controllers;

                public class HomeController : Controller
                {
                    public ActionResult Index()
                    {
                        return View();
                    }

                    public JsonResult Summary()
                    {
                        return Json(new { Message = "Test" });
                    }

                    protected override void OnException(ExceptionContext filterContext)
                    {
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcAction && x.Name == "HomeController.Index");
            Assert.Contains(artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcAction && x.Name == "HomeController.Summary");
            Assert.DoesNotContain(artifacts,
                x => x.Kind == LegacyAspNetArtifactKind.MvcAction && x.Name == "HomeController.OnException");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenMvcControllerHasRouteAttributes_AddsMvcRouteAttributeArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "CustomerController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Controllers;

                [RoutePrefix("customers")]
                public class CustomerController : Controller
                {
                    [Route("{id}")]
                    public ActionResult Details(int id)
                    {
                        return View();
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcRouteAttribute &&
                x.Name == "CustomerController [RoutePrefix]");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcRouteAttribute &&
                x.Name == "CustomerController.Details [Route]");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenMvcControllerHasActionAttributes_AddsMvcActionAttributeArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "HomeController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Controllers;

                public class HomeController : Controller
                {
                    [HttpPost]
                    [ValidateAntiForgeryToken]
                    public ActionResult Save()
                    {
                        return RedirectToAction(nameof(Index));
                    }

                    [AllowAnonymous]
                    public JsonResult Summary()
                    {
                        return Json(new { Message = "Test" });
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                x.Name == "HomeController.Save [HttpPost]");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                x.Name == "HomeController.Save [ValidateAntiForgeryToken]");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                x.Name == "HomeController.Summary [AllowAnonymous]");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenRouteConfigExists_AddsRouteConfigArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var appStartPath = Path.Combine(rootPath, "App_Start");
            Directory.CreateDirectory(appStartPath);

            var routeConfigPath = Path.Combine(appStartPath, "RouteConfig.cs");

            File.WriteAllText(
                routeConfigPath,
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

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.RouteConfig &&
                x.Name == "RouteConfig.cs" &&
                x.FilePath == routeConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenAreaRegistrationExists_AddsAreaRegistrationArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var areaPath = Path.Combine(rootPath, "Areas", "Admin");
            Directory.CreateDirectory(areaPath);

            var areaRegistrationPath = Path.Combine(areaPath, "AdminAreaRegistration.cs");

            File.WriteAllText(
                areaRegistrationPath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Areas.Admin;

                public class AdminAreaRegistration : AreaRegistration
                {
                    public override string AreaName => "Admin";

                    public override void RegisterArea(AreaRegistrationContext context)
                    {
                        context.MapRoute(
                            name: "Admin_default",
                            url: "Admin/{controller}/{action}/{id}");
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.AreaRegistration &&
                x.Name == "AdminAreaRegistration" &&
                x.FilePath == areaRegistrationPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenGlobalAsaxCodeBehindHasMvcStartupRegistration_AddsStartupArtifacts()
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

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcApplicationStartup &&
                x.Name == "Global.asax.cs Application_Start");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcAreaRegistrationCall &&
                x.Name == "AreaRegistration.RegisterAllAreas");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcRouteRegistrationCall &&
                x.Name == "RouteConfig.RegisterRoutes");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcBundleRegistrationCall &&
                x.Name == "BundleConfig.RegisterBundles");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcFilterRegistrationCall &&
                x.Name == "FilterConfig.RegisterGlobalFilters");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenBundleConfigExists_AddsMvcBundleConfigArtifact()
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
                using System.Web.Optimization;

                namespace SampleLegacyApp.Web;

                public static class BundleConfig
                {
                    public static void RegisterBundles(BundleCollection bundles)
                    {
                        bundles.Add(new ScriptBundle("~/bundles/jquery"));
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcBundleConfig &&
                x.Name == "BundleConfig.cs" &&
                x.FilePath == bundleConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenFilterConfigExists_AddsMvcFilterConfigArtifact()
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

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcFilterConfig &&
                x.Name == "FilterConfig.cs" &&
                x.FilePath == filterConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenWebApiControllerExists_AddsWebApiControllerArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "CustomersApiController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Http;

                namespace SampleLegacyApp.Web.Controllers;

                public class CustomersApiController : ApiController
                {
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiController &&
                x.Name == "CustomersApiController" &&
                x.FilePath == controllerPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenWebApiControllerUsesFullyQualifiedApiController_AddsWebApiControllerArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "OrdersApiController.cs");

            File.WriteAllText(
                controllerPath,
                """
                namespace SampleLegacyApp.Web.Controllers;

                public class OrdersApiController : System.Web.Http.ApiController
                {
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiController &&
                x.Name == "OrdersApiController" &&
                x.FilePath == controllerPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenWebApiControllerHasActions_AddsWebApiActionArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "CustomersApiController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Net.Http;
                using System.Web.Http;

                namespace SampleLegacyApp.Web.Controllers;

                public class CustomersApiController : ApiController
                {
                    public IHttpActionResult Get(int id)
                    {
                        return Ok();
                    }

                    public HttpResponseMessage Ping()
                    {
                        return new HttpResponseMessage();
                    }

                    [NonAction]
                    public IHttpActionResult Helper()
                    {
                        return Ok();
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiAction &&
                x.Name == "CustomersApiController.Get");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiAction &&
                x.Name == "CustomersApiController.Ping");

            Assert.DoesNotContain(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiAction &&
                x.Name == "CustomersApiController.Helper");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenWebApiControllerHasRouteAttributes_AddsWebApiRouteAttributeArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "CustomersApiController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Http;

                namespace SampleLegacyApp.Web.Controllers;

                [RoutePrefix("api/customers")]
                public class CustomersApiController : ApiController
                {
                    [Route("{id}")]
                    public IHttpActionResult Get(int id)
                    {
                        return Ok();
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiRouteAttribute &&
                x.Name == "CustomersApiController [RoutePrefix]");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiRouteAttribute &&
                x.Name == "CustomersApiController.Get [Route]");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenWebApiControllerHasActionAttributes_AddsWebApiActionAttributeArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "CustomersApiController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Http;

                namespace SampleLegacyApp.Web.Controllers;

                public class CustomersApiController : ApiController
                {
                    [HttpGet]
                    [Authorize]
                    public IHttpActionResult Get(int id)
                    {
                        return Ok();
                    }

                    [HttpPost]
                    [AllowAnonymous]
                    public IHttpActionResult Create(CustomerDto customer)
                    {
                        return Ok();
                    }
                }

                public sealed class CustomerDto
                {
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiActionAttribute &&
                x.Name == "CustomersApiController.Get [HttpGet]");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiActionAttribute &&
                x.Name == "CustomersApiController.Get [Authorize]");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiActionAttribute &&
                x.Name == "CustomersApiController.Create [HttpPost]");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiActionAttribute &&
                x.Name == "CustomersApiController.Create [AllowAnonymous]");
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenWebApiConfigExists_AddsWebApiConfigAndRouteRegistrationArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var appStartPath = Path.Combine(rootPath, "App_Start");
            Directory.CreateDirectory(appStartPath);

            var webApiConfigPath = Path.Combine(appStartPath, "WebApiConfig.cs");

            File.WriteAllText(
                webApiConfigPath,
                """
                using System.Web.Http;

                namespace SampleLegacyApp.Web;

                public static class WebApiConfig
                {
                    public static void Register(HttpConfiguration config)
                    {
                        config.MapHttpAttributeRoutes();

                        config.Routes.MapHttpRoute(
                            name: "DefaultApi",
                            routeTemplate: "api/{controller}/{id}",
                            defaults: new { id = RouteParameter.Optional });
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiConfig &&
                x.Name == "WebApiConfig.cs" &&
                x.FilePath == webApiConfigPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiRouteRegistrationCall &&
                x.Name == "MapHttpRoute" &&
                x.FilePath == webApiConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenApplicationStartRegistersWebApi_AddsWebApiGlobalConfigurationCallArtifact()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var globalAsaxCodeBehindPath = Path.Combine(rootPath, "Global.asax.cs");

            File.WriteAllText(
                globalAsaxCodeBehindPath,
                """
                using System.Web;
                using System.Web.Http;

                namespace SampleLegacyApp.Web;

                public class MvcApplication : HttpApplication
                {
                    protected void Application_Start()
                    {
                        GlobalConfiguration.Configure(WebApiConfig.Register);
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.MvcApplicationStartup &&
                x.Name == "Global.asax.cs Application_Start");

            Assert.Contains(artifacts, x =>
                x.Kind == LegacyAspNetArtifactKind.WebApiGlobalConfigurationCall &&
                x.Name == "GlobalConfiguration.Configure" &&
                x.FilePath == globalAsaxCodeBehindPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_ReturnsHttpModuleRegistration_WhenSystemWebHttpModuleExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var webConfigPath = Path.Combine(rootPath, "Web.config");

            File.WriteAllText(
                webConfigPath,
                """
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <system.web>
                    <httpModules>
                      <add name="LegacyAuthModule" type="SampleLegacyApp.Web.LegacyAuthModule, SampleLegacyApp.Web" />
                    </httpModules>
                  </system.web>
                </configuration>
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle(x =>
                x.Kind == LegacyAspNetArtifactKind.HttpModuleRegistration &&
                x.Name == "LegacyAuthModule" &&
                x.FilePath == webConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_ReturnsHttpHandlerRegistration_WhenSystemWebHttpHandlerExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var webConfigPath = Path.Combine(rootPath, "Web.config");

            File.WriteAllText(
                webConfigPath,
                """
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <system.web>
                    <httpHandlers>
                      <add path="*.legacy" verb="*" type="SampleLegacyApp.Web.LegacyHandler, SampleLegacyApp.Web" />
                    </httpHandlers>
                  </system.web>
                </configuration>
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle(x =>
                x.Kind == LegacyAspNetArtifactKind.HttpHandlerRegistration &&
                x.Name == "*.legacy" &&
                x.FilePath == webConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_ReturnsHttpModuleRegistration_WhenSystemWebServerModuleExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var webConfigPath = Path.Combine(rootPath, "Web.config");

            File.WriteAllText(
                webConfigPath,
                """
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <system.webServer>
                    <modules>
                      <add name="IntegratedLegacyModule" type="SampleLegacyApp.Web.IntegratedLegacyModule, SampleLegacyApp.Web" />
                    </modules>
                  </system.webServer>
                </configuration>
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle(x =>
                x.Kind == LegacyAspNetArtifactKind.HttpModuleRegistration &&
                x.Name == "IntegratedLegacyModule" &&
                x.FilePath == webConfigPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_ReturnsHttpHandlerRegistration_WhenSystemWebServerHandlerExists()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var webConfigPath = Path.Combine(rootPath, "Web.config");

            File.WriteAllText(
                webConfigPath,
                """
                <?xml version="1.0" encoding="utf-8"?>
                <configuration>
                  <system.webServer>
                    <handlers>
                      <add name="IntegratedLegacyHandler" path="legacy.axd" verb="*" type="SampleLegacyApp.Web.IntegratedLegacyHandler, SampleLegacyApp.Web" />
                    </handlers>
                  </system.webServer>
                </configuration>
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            artifacts.Should().ContainSingle(x =>
                x.Kind == LegacyAspNetArtifactKind.HttpHandlerRegistration &&
                x.Name == "IntegratedLegacyHandler" &&
                x.FilePath == webConfigPath);
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
            $"LegacyLensTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);

        return path;
    }
}