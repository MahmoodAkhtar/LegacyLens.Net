using System.Web.Mvc;

namespace SampleLegacyApp.Web.Controllers;

[RoutePrefix("home")]
public class HomeController : Controller
{
    [HttpGet]
    [Route("")]
    public ActionResult Index()
    {
        return View();
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Route("save")]
    public ActionResult Save()
    {
        return RedirectToAction(nameof(Index));
    }

    [AllowAnonymous]
    [Route("summary")]
    public JsonResult Summary()
    {
        return Json(
            new
            {
                Message = "Sample legacy MVC JSON endpoint"
            },
            JsonRequestBehavior.AllowGet);
    }
}