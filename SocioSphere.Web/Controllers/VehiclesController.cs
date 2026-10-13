using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize]
public class VehiclesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public VehiclesController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index()
    {
        var list = await _db.Vehicles.Include(v => v.Flat).ThenInclude(f => f.Building)
            .OrderBy(v => v.RegistrationNumber).ToListAsync();
        return View("IndexAdmin", list);
    }

    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Mine()
    {
        var uid = _um.GetUserId(User);
        var profile = await _db.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == uid && r.IsActive);
        if (profile == null) return View("Mine", new List<Vehicle>());
        var list = await _db.Vehicles.Where(v => v.FlatId == profile.FlatId && v.IsActive).ToListAsync();
        return View("Mine", list);
    }

    [Authorize(Roles = "Resident")]
    public IActionResult Add() => View(new Vehicle());

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Add(Vehicle vm)
    {
        ModelState.Remove(nameof(Vehicle.Flat));
        var uid = _um.GetUserId(User)!;
        var profile = await _db.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == uid && r.IsActive);
        if (profile == null) { ModelState.AddModelError("", "No flat linked."); return View(vm); }
        if (string.IsNullOrWhiteSpace(vm.RegistrationNumber)) ModelState.AddModelError("", "Registration required.");
        if (await _db.Vehicles.AnyAsync(v => v.RegistrationNumber == vm.RegistrationNumber && v.IsActive))
            ModelState.AddModelError("", "This registration is already registered.");
        if (!ModelState.IsValid) return View(vm);
        vm.FlatId = profile.FlatId; vm.IsActive = true;
        _db.Vehicles.Add(vm);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Vehicle registered.";
        return RedirectToAction(nameof(Mine));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Deactivate(int id)
    {
        var v = await _db.Vehicles.FindAsync(id);
        if (v == null) return NotFound();
        v.IsActive = false;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Vehicle removed.";
        return RedirectToAction(nameof(Mine));
    }
}