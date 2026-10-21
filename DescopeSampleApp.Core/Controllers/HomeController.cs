using Microsoft.AspNetCore.Mvc;

namespace DescopeSampleApp.Core.Controllers;

public sealed class HomeController : Controller
{
    public IActionResult Index()
    {
        ViewBag.Title = "Home Page";
        return View();
    }
}
