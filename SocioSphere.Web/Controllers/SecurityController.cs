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

    private IActionResult Soon(string title) { ViewBag.PageTitle = title; return View("ComingSoon"); }

    public async Task<IActionResult> Dashboard()
    {
        var today = DateTime.UtcNow.Date;
        var expectedAll = await _db.ExpectedVisitors.Include(v => v.Flat).ThenInclude(f => f.Building)
            .Where(v => v.ExpectedDate >= today && v.ExpectedDate < today.AddDays(1)).ToListAsync();
        var expected = expectedAll.OrderBy(v => v.ExpectedTime ?? TimeSpan.Zero).ToList();
        var checkedIn = await _db.Visitors.Include(v => v.Flat)
            .Where(v => v.Status == VisitorStatus.CHECKED_IN).OrderByDescending(v => v.EntryTime).ToListAsync();
        var todayCount = await _db.Visitors.CountAsync(v => v.EntryTime >= today && v.EntryTime < today.AddDays(1));

        ViewBag.Expected = expected;
        ViewBag.CheckedIn = checkedIn;
        ViewBag.TodayCount = todayCount;
        return View();
    }

    public IActionResult ExpectedVisitors() => Soon("Expected Visitors");
    public IActionResult VisitorEntry() => Soon("Visitor Entry");
    public IActionResult History() => Soon("Visitor History");
    public async Task<IActionResult> Emergency()
    {
        ViewBag.Staff = await _db.Staffs.Where(s => s.IsActive).OrderBy(s => s.Role).ToListAsync();
        return View();
    }
    public IActionResult Profile() => Soon("My Profile");
}


