using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class VendorsController : Controller
{
    private readonly ApplicationDbContext _db;
    public VendorsController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Vendors.OrderBy(v => v.Name).ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string? service, string? phone, string? email)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Name required."; return RedirectToAction(nameof(Index)); }
        _db.Vendors.Add(new Vendor { Name = name, Service = service, Phone = phone, Email = email, Status = VendorStatus.ACTIVE, ContractStart = DateTime.UtcNow });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Vendor added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Toggle(int id)
    {
        var v = await _db.Vendors.FindAsync(id);
        if (v == null) return NotFound();
        v.Status = v.Status == VendorStatus.ACTIVE ? VendorStatus.INACTIVE : VendorStatus.ACTIVE;
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }
}