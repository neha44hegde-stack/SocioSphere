using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Security,Admin,Resident")]
public class VisitorsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public VisitorsController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    [Authorize(Roles = "Security,Admin")]
    public async Task<IActionResult> History(int? flatId, DateTime? from, VisitorType? type)
    {
        var q = _db.Visitors.Include(v => v.Flat).ThenInclude(f => f.Building).AsQueryable();
        if (flatId.HasValue) q = q.Where(v => v.FlatId == flatId.Value);
        if (from.HasValue) q = q.Where(v => v.EntryTime >= from.Value.Date);
        if (type.HasValue) q = q.Where(v => v.Type == type.Value);
        var list = await q.OrderByDescending(v => v.EntryTime).Take(200).ToListAsync();
        ViewBag.FlatId = flatId; ViewBag.From = from; ViewBag.Type = type;
        ViewBag.Flats = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _db.Flats.OrderBy(f => f.FlatNumber).ToListAsync(), "Id", "FlatNumber");
        return View(list);
    }

    [Authorize(Roles = "Security,Admin")]
    public async Task<IActionResult> Register()
    {
        ViewBag.Flats = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _db.Flats.OrderBy(f => f.FlatNumber).ToListAsync(), "Id", "FlatNumber");
        var today = DateTime.Today;
        ViewBag.Expected = await _db.ExpectedVisitors.Include(v => v.Flat)
            .Where(v => v.ExpectedDate.Year == today.Year && v.ExpectedDate.Month == today.Month && v.ExpectedDate.Day == today.Day).ToListAsync();
        return View(new Visitor());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Security,Admin")]
    public async Task<IActionResult> Register(Visitor vm)
    {
        ModelState.Remove(nameof(Visitor.Flat));
        if (string.IsNullOrWhiteSpace(vm.Name)) ModelState.AddModelError("", "Name is required.");
        if (vm.FlatId <= 0) ModelState.AddModelError("", "Flat is required.");
        if (!ModelState.IsValid)
        {
            ViewBag.Flats = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Flats.OrderBy(f => f.FlatNumber).ToListAsync(), "Id", "FlatNumber");
            return View(vm);
        }
        vm.EntryTime = DateTime.UtcNow;
        vm.Status = VisitorStatus.CHECKED_IN;
        vm.RegisteredByUserId = _um.GetUserId(User);
        _db.Visitors.Add(vm);
        await _db.SaveChangesAsync();
        TempData["Success"] = vm.Name + " checked in.";
        return RedirectToAction(nameof(History));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Security,Admin")]
    public async Task<IActionResult> Checkout(int id)
    {
        var v = await _db.Visitors.FindAsync(id);
        if (v == null) return NotFound();
        v.ExitTime = DateTime.UtcNow;
        v.Status = VisitorStatus.CHECKED_OUT;
        await _db.SaveChangesAsync();
        TempData["Success"] = v.Name + " checked out.";
        return RedirectToAction(nameof(History));
    }

    [Authorize(Roles = "Security,Admin")]
    public async Task<IActionResult> Expected()
    {
        var today = DateTime.Today;
        var raw = await _db.ExpectedVisitors.Include(v => v.Flat).ThenInclude(f => f.Building)
            .Where(v => v.ExpectedDate >= today).ToListAsync();
        var list = raw.OrderBy(v => v.ExpectedDate).ThenBy(v => v.ExpectedTime ?? TimeSpan.Zero).ToList();
        return View(list);
    }

    [Authorize(Roles = "Resident")]
    public IActionResult AddExpected() => View(new ExpectedVisitor { ExpectedDate = DateTime.Today });

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> AddExpected(ExpectedVisitor vm)
    {
        ModelState.Remove(nameof(ExpectedVisitor.Flat));
        var uid = _um.GetUserId(User)!;
        var profile = await _db.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == uid && r.IsActive);
        if (profile == null) { ModelState.AddModelError("", "No flat linked."); return View(vm); }
        if (string.IsNullOrWhiteSpace(vm.Name)) ModelState.AddModelError("", "Name required.");
        if (vm.ExpectedDate.Date < DateTime.Today) ModelState.AddModelError("", "Date cannot be in the past.");
        if (!ModelState.IsValid) return View(vm);
        vm.FlatId = profile.FlatId;
        vm.CreatedByUserId = uid;
        _db.ExpectedVisitors.Add(vm);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Expected visitor added.";
        return RedirectToAction(nameof(MyExpected));
    }

    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> MyExpected()
    {
        var uid = _um.GetUserId(User);
        var list = await _db.ExpectedVisitors.Where(v => v.CreatedByUserId == uid)
            .OrderByDescending(v => v.ExpectedDate).ToListAsync();
        return View(list);
    }

    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Mine()
    {
        var uid = _um.GetUserId(User);
        var profile = await _db.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == uid && r.IsActive);
        if (profile == null) return View(new List<Visitor>());
        var list = await _db.Visitors.Where(v => v.FlatId == profile.FlatId)
            .OrderByDescending(v => v.EntryTime).Take(100).ToListAsync();
        return View(list);
    }
}

