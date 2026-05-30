using System.Web.Mvc;

namespace SampleLegacyApp.Web.Controllers;

public class HomeController : Controller
{
    public ActionResult Index()
    {
        return View();
    }
}
