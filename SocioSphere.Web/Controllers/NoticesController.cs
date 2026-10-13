using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize]
public class NoticesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public NoticesController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    // Admin list (all notices, including expired)
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(NoticeCategory? category)
    {
        var q = _db.Notices.AsQueryable();
        if (category.HasValue) q = q.Where(n => n.Category == category.Value);
        var list = await q.OrderByDescending(n => n.PublishedAt).Take(200).ToListAsync();
        ViewBag.Category = category;
        return View(list);
    }

    // Resident: only active notices
    [Authorize(Roles = "Resident,Admin")]
    public async Task<IActionResult> Active(NoticeCategory? category)
    {
        var now = DateTime.UtcNow;
        var q = _db.Notices.Where(n => n.ExpiresAt == null || n.ExpiresAt > now);
        if (category.HasValue) q = q.Where(n => n.Category == category.Value);
        var list = await q.OrderByDescending(n => n.PublishedAt).ToListAsync();
        ViewBag.Category = category;
        return View(list);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Notice());

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Notice vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Title) || string.IsNullOrWhiteSpace(vm.Content))
            ModelState.AddModelError("", "Title and Content are required.");

        if (!ModelState.IsValid) return View(vm);

        vm.CreatedByUserId = _um.GetUserId(User)!;
        vm.PublishedAt = DateTime.UtcNow;
        _db.Notices.Add(vm);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Notice published.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var n = await _db.Notices.FindAsync(id);
        if (n == null) return NotFound();
        return View(n);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Notice vm)
    {
        var n = await _db.Notices.FindAsync(vm.Id);
        if (n == null) return NotFound();

        n.Title = vm.Title;
        n.Content = vm.Content;
        n.Category = vm.Category;
        n.Priority = vm.Priority;
        n.ExpiresAt = vm.ExpiresAt;

        await _db.SaveChangesAsync();
        TempData["Success"] = "Notice updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var n = await _db.Notices.FindAsync(id);
        if (n == null) return NotFound();
        _db.Notices.Remove(n);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Notice deleted.";
        return RedirectToAction(nameof(Index));
    }

    // Details view (both roles)
    public async Task<IActionResult> Details(int id)
    {
        var n = await _db.Notices.FindAsync(id);
        if (n == null) return NotFound();

        if (User.IsInRole("Resident"))
        {
            if (n.ExpiresAt.HasValue && n.ExpiresAt < DateTime.UtcNow)
                return Forbid();
        }
        return View(n);
    }
}
