using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ParkingController : Controller
{
    private readonly ApplicationDbContext _db;
    public ParkingController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var list = await _db.ParkingSlots.Include(p => p.Flat).ThenInclude(f => f.Building).OrderBy(p => p.SlotNumber).ToListAsync();
        ViewBag.Flats = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _db.Flats.Include(f => f.Building).OrderBy(f => f.FlatNumber).ToListAsync(), "Id", "FlatNumber");
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string slotNumber, ParkingType type)
    {
        if (string.IsNullOrWhiteSpace(slotNumber)) { TempData["Error"] = "Slot number required."; return RedirectToAction(nameof(Index)); }
        if (await _db.ParkingSlots.AnyAsync(p => p.SlotNumber == slotNumber)) { TempData["Error"] = "Slot already exists."; return RedirectToAction(nameof(Index)); }
        _db.ParkingSlots.Add(new ParkingSlot { SlotNumber = slotNumber, Type = type, Status = ParkingStatus.AVAILABLE });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Slot added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Assign(int id, int flatId, string? vehicleNumber)
    {
        var slot = await _db.ParkingSlots.FindAsync(id);
        if (slot == null) return NotFound();
        slot.FlatId = flatId; slot.VehicleNumber = vehicleNumber; slot.Status = ParkingStatus.ALLOCATED;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Slot assigned.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Release(int id)
    {
        var slot = await _db.ParkingSlots.FindAsync(id);
        if (slot == null) return NotFound();
        slot.FlatId = null; slot.VehicleNumber = null; slot.Status = ParkingStatus.AVAILABLE;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Slot released.";
        return RedirectToAction(nameof(Index));
    }
}