using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using SocioSphere.Core.Entities;

namespace SocioSphere.Web.Controllers;

public class HomeController : Controller
{
    private readonly UserManager<ApplicationUser> _um;
    public HomeController(UserManager<ApplicationUser> um) { _um = um; }

    public IActionResult Index()
    {
        if (User.Identity?.IsAuthenticated == true)
        {
            if (User.IsInRole("Admin")) return RedirectToAction("Dashboard", "Admin");
            if (User.IsInRole("Resident")) return RedirectToAction("Dashboard", "Resident");
            if (User.IsInRole("Security")) return RedirectToAction("Dashboard", "Security");
        }
        return View();
    }

    public IActionResult Privacy() => View();
}
