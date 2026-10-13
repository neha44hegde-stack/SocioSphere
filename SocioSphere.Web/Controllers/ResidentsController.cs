using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ResidentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public ResidentsController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    public async Task<IActionResult> Index(string? q)
    {
        var query = _db.ResidentProfiles.Include(r => r.User).Include(r => r.Flat).ThenInclude(f => f.Building)
            .Where(r => r.IsActive);
        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(r => r.User.Name.Contains(q) || r.Flat.FlatNumber.Contains(q));
        var list = await query.OrderBy(r => r.Flat.FlatNumber).ToListAsync();
        ViewBag.Query = q;
        return View(list);
    }

    public async Task<IActionResult> Create()
    {
        var flats = await _db.Flats.Include(f => f.Building).OrderBy(f => f.FlatNumber).ToListAsync();
        ViewBag.Flats = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(flats, "Id", "FlatNumber");
        return View();
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, string email, string phone, string password, int flatId, RelationshipType relationship, bool isPrimary)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            ModelState.AddModelError("", "Email and password required.");
        if (await _um.FindByEmailAsync(email) != null)
            ModelState.AddModelError("", "Email already in use.");
        if (isPrimary && await _db.ResidentProfiles.AnyAsync(r => r.FlatId == flatId && r.IsPrimaryResident && r.IsActive))
            ModelState.AddModelError("", "This flat already has a primary resident.");

        if (!ModelState.IsValid)
        {
            ViewBag.Flats = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
                await _db.Flats.Include(f => f.Building).OrderBy(f => f.FlatNumber).ToListAsync(), "Id", "FlatNumber");
            return View();
        }

        var u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, Name = name, Phone = phone };
        var r = await _um.CreateAsync(u, password);
        if (!r.Succeeded) { ModelState.AddModelError("", string.Join("; ", r.Errors.Select(e => e.Description))); return View(); }
        await _um.AddToRoleAsync(u, "Resident");

        _db.ResidentProfiles.Add(new ResidentProfile { UserId = u.Id, FlatId = flatId, Relationship = relationship, IsPrimaryResident = isPrimary });
        var flat = await _db.Flats.FindAsync(flatId);
        if (flat != null) flat.Status = FlatStatus.OCCUPIED;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Resident added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Deactivate(int id)
    {
        var rp = await _db.ResidentProfiles.FindAsync(id);
        if (rp == null) return NotFound();
        rp.IsActive = false;
        await _db.SaveChangesAsync();
        var stillActive = await _db.ResidentProfiles.AnyAsync(x => x.FlatId == rp.FlatId && x.IsActive);
        if (!stillActive)
        {
            var flat = await _db.Flats.FindAsync(rp.FlatId);
            if (flat != null) flat.Status = FlatStatus.VACANT;
            await _db.SaveChangesAsync();
        }
        TempData["Success"] = "Resident deactivated.";
        return RedirectToAction(nameof(Index));
    }
}