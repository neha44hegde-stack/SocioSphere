using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize]
public class AmenitiesController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public AmenitiesController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    // Everyone can view active amenities
    public async Task<IActionResult> Index()
    {
        var list = await _db.Amenities.Where(a => a.IsActive || User.IsInRole("Admin"))
            .OrderBy(a => a.Name).ToListAsync();
        return View(list);
    }

    [Authorize(Roles = "Admin")]
    public IActionResult Create() => View(new Amenity
    {
        OpeningTime = TimeSpan.FromHours(9),
        ClosingTime = TimeSpan.FromHours(22),
        MaxBookingMinutes = 120,
        Capacity = 20
    });

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(Amenity vm)
    {
        if (string.IsNullOrWhiteSpace(vm.Name)) ModelState.AddModelError("", "Name is required.");
        if (vm.ClosingTime <= vm.OpeningTime) ModelState.AddModelError("", "Closing time must be after opening time.");
        if (!ModelState.IsValid) return View(vm);
        _db.Amenities.Add(vm);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Amenity added.";
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(int id)
    {
        var a = await _db.Amenities.FindAsync(id);
        if (a == null) return NotFound();
        return View(a);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Edit(Amenity vm)
    {
        var a = await _db.Amenities.FindAsync(vm.Id);
        if (a == null) return NotFound();
        a.Name = vm.Name; a.Description = vm.Description; a.Capacity = vm.Capacity;
        a.OpeningTime = vm.OpeningTime; a.ClosingTime = vm.ClosingTime;
        a.MaxBookingMinutes = vm.MaxBookingMinutes; a.IsActive = vm.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Amenity updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Toggle(int id)
    {
        var a = await _db.Amenities.FindAsync(id);
        if (a == null) return NotFound();
        a.IsActive = !a.IsActive;
        await _db.SaveChangesAsync();
        TempData["Success"] = a.IsActive ? "Amenity activated." : "Amenity deactivated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- Booking ----------
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Book(int id)
    {
        var a = await _db.Amenities.FindAsync(id);
        if (a == null || !a.IsActive) return NotFound();
        ViewBag.Amenity = a;
        return View(new AmenityBooking { BookingDate = DateTime.Today.AddDays(1) });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Book(int id, DateTime bookingDate, string startTime, string endTime, string? purpose)
    {
        var a = await _db.Amenities.FindAsync(id);
        if (a == null || !a.IsActive) return NotFound();

        ViewBag.Amenity = a;

        if (!TimeSpan.TryParse(startTime, out var s) || !TimeSpan.TryParse(endTime, out var e))
        { ModelState.AddModelError("", "Invalid time."); return View(); }

        if (bookingDate.Date < DateTime.Today)
            ModelState.AddModelError("", "Cannot book a date in the past.");
        if (s < a.OpeningTime || e > a.ClosingTime)
            ModelState.AddModelError("", $"Booking must be between {a.OpeningTime:hh\\:mm} and {a.ClosingTime:hh\\:mm}.");
        if (e <= s)
            ModelState.AddModelError("", "End time must be after start time.");
        if ((e - s).TotalMinutes > a.MaxBookingMinutes)
            ModelState.AddModelError("", $"Maximum booking duration is {a.MaxBookingMinutes} minutes.");

        var sameDay = bookingDate.Date;
        var bookingsThatDay = await _db.AmenityBookings
            .Where(b => b.AmenityId == id
                && b.BookingDate.Year == sameDay.Year
                && b.BookingDate.Month == sameDay.Month
                && b.BookingDate.Day == sameDay.Day)
            .ToListAsync();
        var overlap = bookingsThatDay.Any(b =>
            b.Status == BookingStatus.CONFIRMED && b.StartTime < e && b.EndTime > s);
        if (overlap) ModelState.AddModelError("", "That time slot is already booked.");

        if (!ModelState.IsValid) return View();

        _db.AmenityBookings.Add(new AmenityBooking
        {
            AmenityId = id,
            ResidentUserId = _um.GetUserId(User)!,
            BookingDate = bookingDate.Date,
            StartTime = s,
            EndTime = e,
            Purpose = purpose,
            Status = BookingStatus.CONFIRMED
        });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Booking confirmed.";
        return RedirectToAction("MyBookings");
    }

    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> MyBookings()
    {
        var uid = _um.GetUserId(User);
        var list = await _db.AmenityBookings.Include(b => b.Amenity)
            .Where(b => b.ResidentUserId == uid)
            .OrderByDescending(b => b.BookingDate).ToListAsync();
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Cancel(int id)
    {
        var b = await _db.AmenityBookings.FindAsync(id);
        if (b == null) return NotFound();
        if (b.ResidentUserId != _um.GetUserId(User)) return Forbid();
        b.Status = BookingStatus.CANCELLED;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Booking cancelled.";
        return RedirectToAction("MyBookings");
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> AllBookings()
    {
        var list = await _db.AmenityBookings.Include(b => b.Amenity).Include(b => b.Resident)
            .OrderByDescending(b => b.BookingDate).Take(200).ToListAsync();
        return View(list);
    }
}
