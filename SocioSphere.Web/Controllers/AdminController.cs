using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _db;
    public AdminController(ApplicationDbContext db) { _db = db; }

    private IActionResult Soon(string title) { ViewBag.PageTitle = title; return View("ComingSoon"); }

    public async Task<IActionResult> Dashboard()
    {
        var totalFlats = await _db.Flats.CountAsync();
        var occupied = await _db.Flats.CountAsync(f => f.Status == FlatStatus.OCCUPIED);
        var residents = await _db.ResidentProfiles.CountAsync(r => r.IsActive);
        var pendingBills = await _db.MaintenanceBills.CountAsync(b => b.Status != BillStatus.PAID);

        var unpaid = await _db.MaintenanceBills.Where(b => b.Status != BillStatus.PAID).Select(b => b.TotalAmount).ToListAsync();
        var outstanding = unpaid.Sum();

        var openComplaints = await _db.Complaints.CountAsync(c => c.Status != ComplaintStatus.RESOLVED && c.Status != ComplaintStatus.CLOSED);
        var urgentComplaints = await _db.Complaints.CountAsync(c => c.Priority == ComplaintPriority.URGENT && c.Status != ComplaintStatus.CLOSED);
        var today = DateTime.UtcNow.Date;
        var todaysVisitors = await _db.Visitors.CountAsync(v => v.EntryTime >= today && v.EntryTime < today.AddDays(1));
        var upcoming = await _db.AmenityBookings.CountAsync(b => b.BookingDate >= today && b.Status == BookingStatus.CONFIRMED);

        ViewBag.TotalFlats = totalFlats;
        ViewBag.Occupied = occupied;
        ViewBag.Vacant = totalFlats - occupied;
        ViewBag.Residents = residents;
        ViewBag.PendingBills = pendingBills;
        ViewBag.Outstanding = outstanding;
        ViewBag.OpenComplaints = openComplaints;
        ViewBag.UrgentComplaints = urgentComplaints;
        ViewBag.TodaysVisitors = todaysVisitors;
        ViewBag.UpcomingBookings = upcoming;

        ViewBag.RecentPayments = await _db.Payments.Include(p => p.MaintenanceBill).ThenInclude(b => b.Flat).OrderByDescending(p => p.PaymentDate).Take(5).ToListAsync();
        ViewBag.RecentComplaints = await _db.Complaints.Include(c => c.Flat).OrderByDescending(c => c.CreatedAt).Take(5).ToListAsync();
        ViewBag.RecentNotices = await _db.Notices.OrderByDescending(n => n.PublishedAt).Take(5).ToListAsync();
        ViewBag.RecentVisitors = await _db.Visitors.Include(v => v.Flat).OrderByDescending(v => v.EntryTime).Take(5).ToListAsync();

        return View();
    }

    public IActionResult Buildings() => Soon("Buildings & Flats");
    public IActionResult Residents() => Soon("Residents");
    public IActionResult Complaints() => Soon("Complaints");
    public IActionResult Notices() => Soon("Notices");
    public IActionResult Amenities() => Soon("Amenities");
    public IActionResult Visitors() => Soon("Visitors");
    public IActionResult Parking() => Soon("Parking");
    public IActionResult Vehicles() => Soon("Vehicles");
    public IActionResult Staff() => Soon("Staff");
    public IActionResult Vendors() => Soon("Vendors");
    public IActionResult Expenses() => Soon("Society Expenses");
    public IActionResult Reports() => Soon("Reports");
    public IActionResult Settings() => Soon("Settings");
}
