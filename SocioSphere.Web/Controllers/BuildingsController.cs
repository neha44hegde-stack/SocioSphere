using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class BuildingsController : Controller
{
    private readonly ApplicationDbContext _db;
    public BuildingsController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var list = await _db.Buildings.Include(b => b.Flats).OrderBy(b => b.Name).ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, int totalFloors)
    {
        if (string.IsNullOrWhiteSpace(name)) { TempData["Error"] = "Name required."; return RedirectToAction(nameof(Index)); }
        var societyId = (await _db.Societies.FirstOrDefaultAsync())?.Id ?? 1;
        _db.Buildings.Add(new Building { Name = name, TotalFloors = totalFloors, SocietyId = societyId });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Building added.";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Flats(int id)
    {
        var b = await _db.Buildings.Include(x => x.Flats).FirstOrDefaultAsync(x => x.Id == id);
        if (b == null) return NotFound();
        return View(b);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> AddFlat(int buildingId, string flatNumber, int floor, string flatType, decimal area)
    {
        if (string.IsNullOrWhiteSpace(flatNumber)) { TempData["Error"] = "Flat number required."; return RedirectToAction(nameof(Flats), new { id = buildingId }); }
        if (await _db.Flats.AnyAsync(f => f.BuildingId == buildingId && f.FlatNumber == flatNumber))
        { TempData["Error"] = "Flat number already exists in this building."; return RedirectToAction(nameof(Flats), new { id = buildingId }); }
        _db.Flats.Add(new Flat { BuildingId = buildingId, FlatNumber = flatNumber, Floor = floor, FlatType = flatType, Area = area });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Flat added.";
        return RedirectToAction(nameof(Flats), new { id = buildingId });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteFlat(int id)
    {
        var f = await _db.Flats.Include(x => x.Residents).FirstOrDefaultAsync(x => x.Id == id);
        if (f == null) return NotFound();
        if (f.Residents.Any(r => r.IsActive))
        {
            TempData["Error"] = "Cannot delete flat with active residents.";
            return RedirectToAction(nameof(Flats), new { id = f.BuildingId });
        }
        var bid = f.BuildingId;
        _db.Flats.Remove(f);
        await _db.SaveChangesAsync();
        return RedirectToAction(nameof(Flats), new { id = bid });
    }
}