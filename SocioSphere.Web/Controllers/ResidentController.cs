using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Resident")]
public class ResidentController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public ResidentController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    private IActionResult Soon(string title) { ViewBag.PageTitle = title; return View("ComingSoon"); }

    public async Task<IActionResult> Dashboard()
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");

        var profile = await _db.ResidentProfiles.Include(r => r.Flat).ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);

        ViewBag.UserName = user.Name;
        ViewBag.Profile = profile;

        if (profile == null) return View();

        var bills = await _db.MaintenanceBills.Where(b => b.FlatId == profile.FlatId)
            .OrderByDescending(b => b.BillingMonth).ToListAsync();

        var outstanding = bills.Where(b => b.Status != BillStatus.PAID).Sum(b => b.TotalAmount);

        var lastPayment = await _db.Payments.Include(p => p.MaintenanceBill)
            .Where(p => p.MaintenanceBill.FlatId == profile.FlatId && p.Status == PaymentStatus.SUCCESS)
            .OrderByDescending(p => p.PaymentDate).FirstOrDefaultAsync();

        var openComplaints = await _db.Complaints
            .Where(c => c.FlatId == profile.FlatId && c.Status != ComplaintStatus.CLOSED)
            .OrderByDescending(c => c.CreatedAt).ToListAsync();

        var upcomingBookings = await _db.AmenityBookings.Include(b => b.Amenity)
            .Where(b => b.ResidentUserId == user.Id && b.BookingDate >= DateTime.UtcNow.Date && b.Status == BookingStatus.CONFIRMED)
            .OrderBy(b => b.BookingDate).Take(3).ToListAsync();

        var notices = await _db.Notices.Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow)
            .OrderByDescending(n => n.PublishedAt).Take(5).ToListAsync();

        ViewBag.Bills = bills;
        ViewBag.CurrentBill = bills.FirstOrDefault();
        ViewBag.Outstanding = outstanding;
        ViewBag.LastPayment = lastPayment;
        ViewBag.OpenComplaints = openComplaints;
        ViewBag.UpcomingBookings = upcomingBookings;
        ViewBag.Notices = notices;

        return View();
    }

    public async Task<IActionResult> MyFlat()
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");
        var profile = await _db.ResidentProfiles
            .Include(r => r.Flat).ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);
        ViewBag.Vehicles = profile == null
            ? new List<SocioSphere.Core.Entities.Vehicle>()
            : await _db.Vehicles.Where(v => v.FlatId == profile.FlatId && v.IsActive).ToListAsync();
        ViewBag.ParkingSlots = profile == null
            ? new List<SocioSphere.Core.Entities.ParkingSlot>()
            : await _db.ParkingSlots.Where(p => p.FlatId == profile.FlatId).ToListAsync();
        ViewBag.CoResidents = profile == null
            ? new List<SocioSphere.Core.Entities.ResidentProfile>()
            : await _db.ResidentProfiles.Include(r => r.User)
                .Where(r => r.FlatId == profile.FlatId && r.IsActive && r.UserId != user.Id).ToListAsync();
        ViewBag.Bills = profile == null
            ? new List<SocioSphere.Core.Entities.MaintenanceBill>()
            : await _db.MaintenanceBills.Where(b => b.FlatId == profile.FlatId)
                .OrderByDescending(b => b.BillingMonth).Take(5).ToListAsync();
        return View(profile);
    }
    public IActionResult Maintenance() => Soon("My Maintenance");
    public IActionResult Payments() => Soon("My Payments");
    public IActionResult Complaints() => Soon("My Complaints");
    public IActionResult Notices() => Soon("Notices");
    public IActionResult Amenities() => Soon("Amenities");
    public IActionResult Visitors() => Soon("My Visitors");
    public IActionResult Vehicles() => Soon("My Vehicles");
            public async Task<IActionResult> MyBills()
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");

        var profile = await _db.ResidentProfiles
            .Include(r => r.Flat).ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);

        ViewBag.Profile = profile;

        if (profile == null)
            return View(new List<SocioSphere.Core.Entities.MaintenanceBill>());

        var bills = await _db.MaintenanceBills
            .Include(b => b.Payments)
            .Where(b => b.FlatId == profile.FlatId)
            .OrderByDescending(b => b.BillingMonth)
            .ToListAsync();

        ViewBag.Outstanding = bills.Where(b => b.Status != SocioSphere.Core.Enums.BillStatus.PAID).Sum(b => b.TotalAmount - b.Payments.Where(p => p.Status == SocioSphere.Core.Enums.PaymentStatus.SUCCESS).Sum(p => p.Amount));
        ViewBag.TotalBilled = bills.Sum(b => b.TotalAmount);
        ViewBag.TotalPaid = bills.SelectMany(b => b.Payments).Where(p => p.Status == SocioSphere.Core.Enums.PaymentStatus.SUCCESS).Sum(p => p.Amount);

        return View(bills);
    }

    public async Task<IActionResult> MyPayments()
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");

        var profile = await _db.ResidentProfiles
            .Include(r => r.Flat).ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);

        ViewBag.Profile = profile;

        if (profile == null)
            return View(new List<SocioSphere.Core.Entities.Payment>());

        var payments = await _db.Payments
            .Include(p => p.MaintenanceBill)
            .Where(p => p.MaintenanceBill.FlatId == profile.FlatId)
            .OrderByDescending(p => p.PaymentDate)
            .Take(200)
            .ToListAsync();

        ViewBag.TotalPaid = payments.Where(p => p.Status == SocioSphere.Core.Enums.PaymentStatus.SUCCESS).Sum(p => p.Amount);

        return View(payments);
    }

    public IActionResult Profile() => Soon("My Profile");
}


