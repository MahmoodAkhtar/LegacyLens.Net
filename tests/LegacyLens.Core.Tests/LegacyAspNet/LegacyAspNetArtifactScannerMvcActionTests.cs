using LegacyLens.Core.LegacyAspNet;

namespace LegacyLens.Core.Tests.LegacyAspNet;

public sealed class LegacyAspNetArtifactScannerMvcActionTests
{
    [Fact]
    public void Scan_WhenMvcControllerHasActionMethods_AddsMvcActionArtifacts()
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

                    public JsonResult Search()
                    {
                        return Json(new { success = true });
                    }

                    private ActionResult Helper()
                    {
                        return View();
                    }

                    public string NotAnMvcAction()
                    {
                        return "Not an MVC action result";
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcAction &&
                    artifact.Name == "HomeController.Index" &&
                    artifact.FilePath == controllerPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcAction &&
                    artifact.Name == "HomeController.Search" &&
                    artifact.FilePath == controllerPath);

            Assert.DoesNotContain(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcAction &&
                    artifact.Name == "HomeController.Helper");

            Assert.DoesNotContain(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcAction &&
                    artifact.Name == "HomeController.NotAnMvcAction");
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

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcRouteAttribute &&
                    artifact.Name == "CustomerController [RoutePrefix]" &&
                    artifact.FilePath == controllerPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcRouteAttribute &&
                    artifact.Name == "CustomerController.Details [Route]" &&
                    artifact.FilePath == controllerPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    [Fact]
    public void Scan_WhenMvcActionHasCommonActionAttributes_AddsMvcActionAttributeArtifacts()
    {
        var rootPath = CreateTemporaryDirectory();

        try
        {
            var controllerPath = Path.Combine(rootPath, "AccountController.cs");

            File.WriteAllText(
                controllerPath,
                """
                using System.Web.Mvc;

                namespace SampleLegacyApp.Web.Controllers;

                public class AccountController : Controller
                {
                    [HttpPost]
                    [Authorize]
                    [ValidateAntiForgeryToken]
                    public ActionResult Save()
                    {
                        return View();
                    }

                    [AllowAnonymous]
                    [OutputCache(Duration = 60)]
                    public ActionResult Login()
                    {
                        return View();
                    }
                }
                """);

            var scanner = new LegacyAspNetArtifactScanner();

            var artifacts = scanner.Scan(rootPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                    artifact.Name == "AccountController.Save [HttpPost]" &&
                    artifact.FilePath == controllerPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                    artifact.Name == "AccountController.Save [Authorize]" &&
                    artifact.FilePath == controllerPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                    artifact.Name == "AccountController.Save [ValidateAntiForgeryToken]" &&
                    artifact.FilePath == controllerPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                    artifact.Name == "AccountController.Login [AllowAnonymous]" &&
                    artifact.FilePath == controllerPath);

            Assert.Contains(
                artifacts,
                artifact =>
                    artifact.Kind == LegacyAspNetArtifactKind.MvcActionAttribute &&
                    artifact.Name == "AccountController.Login [OutputCache]" &&
                    artifact.FilePath == controllerPath);
        }
        finally
        {
            Directory.Delete(rootPath, recursive: true);
        }
    }

    private static string CreateTemporaryDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), $"LegacyLensTests_{Guid.NewGuid():N}");

        Directory.CreateDirectory(path);

        return path;
    }
}