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

    public async Task<IActionResult> Dashboard()
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");

        var profile = await _db.ResidentProfiles
            .Include(r => r.Flat).ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);

        ViewBag.UserName = user.Name;
        ViewBag.Profile = profile;

        if (profile == null) return View();

        var bills = await _db.MaintenanceBills
            .Where(b => b.FlatId == profile.FlatId)
            .OrderByDescending(b => b.BillingMonth).ToListAsync();

        var outstanding = bills.Where(b => b.Status != BillStatus.PAID).Sum(b => b.TotalAmount);

        var lastPayment = await _db.Payments
            .Include(p => p.MaintenanceBill)
            .Where(p => p.MaintenanceBill.FlatId == profile.FlatId && p.Status == PaymentStatus.SUCCESS)
            .OrderByDescending(p => p.PaymentDate).FirstOrDefaultAsync();

        var openComplaints = await _db.Complaints
            .Where(c => c.FlatId == profile.FlatId && c.Status != ComplaintStatus.CLOSED)
            .OrderByDescending(c => c.CreatedAt).ToListAsync();

        var upcomingBookings = await _db.AmenityBookings
            .Include(b => b.Amenity)
            .Where(b => b.ResidentUserId == user.Id && b.BookingDate >= DateTime.UtcNow.Date && b.Status == BookingStatus.CONFIRMED)
            .OrderBy(b => b.BookingDate).Take(3).ToListAsync();

        var notices = await _db.Notices
            .Where(n => n.ExpiresAt == null || n.ExpiresAt > DateTime.UtcNow)
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

    public IActionResult MyFlat() => View();
    public IActionResult Maintenance() => View();
    public IActionResult Payments() => View();
    public IActionResult Complaints() => View();
    public IActionResult Notices() => View();
    public IActionResult Amenities() => View();
    public IActionResult Visitors() => View();
    public IActionResult Vehicles() => View();
    public IActionResult Profile() => View();
}
