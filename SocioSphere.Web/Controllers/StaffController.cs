using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class StaffController : Controller
{
    private readonly ApplicationDbContext _db;
    public StaffController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Staffs.OrderBy(s => s.Name).ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, StaffRole role, string? phone, string? shift)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Name required."; return RedirectToAction(nameof(Index)); }
        _db.Staffs.Add(new Staff { Name = name, Role = role, Phone = phone, Shift = shift, JoiningDate = DateTime.UtcNow, IsActive = true });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Staff added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var s = await _db.Staffs.FindAsync(id);
        if (s == null) return NotFound();
        s.IsActive = !s.IsActive;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}