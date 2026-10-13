using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Security")]
public class SecurityController : Controller
{
    private readonly ApplicationDbContext _db;
    public SecurityController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Dashboard()
    {
        var today = DateTime.UtcNow.Date;
        var expected = await _db.ExpectedVisitors
            .Include(v => v.Flat).ThenInclude(f => f.Building)
            .Where(v => v.ExpectedDate.Date == today)
            .OrderBy(v => v.ExpectedTime).ToListAsync();
        var checkedIn = await _db.Visitors
            .Include(v => v.Flat)
            .Where(v => v.Status == VisitorStatus.CHECKED_IN)
            .OrderByDescending(v => v.EntryTime).ToListAsync();
        var todayCount = await _db.Visitors.CountAsync(v => v.EntryTime.Date == today);

        ViewBag.Expected = expected;
        ViewBag.CheckedIn = checkedIn;
        ViewBag.TodayCount = todayCount;
        return View();
    }

    public IActionResult ExpectedVisitors() => View();
    public IActionResult VisitorEntry() => View();
    public IActionResult History() => View();
    public IActionResult Emergency() => View();
    public IActionResult Profile() => View();
}
