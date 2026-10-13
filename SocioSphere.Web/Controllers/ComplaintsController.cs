using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize]
public class ComplaintsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public ComplaintsController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    // ---- Admin list ----
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(ComplaintStatus? status, ComplaintPriority? priority, ComplaintCategory? category)
    {
        var q = _db.Complaints
            .Include(c => c.Flat).ThenInclude(f => f.Building)
            .Include(c => c.RaisedBy)
            .Include(c => c.AssignedStaff)
            .AsQueryable();

        if (status.HasValue) q = q.Where(c => c.Status == status.Value);
        if (priority.HasValue) q = q.Where(c => c.Priority == priority.Value);
        if (category.HasValue) q = q.Where(c => c.Category == category.Value);

        var list = await q.OrderByDescending(c => c.CreatedAt).Take(200).ToListAsync();
        ViewBag.Status = status; ViewBag.Priority = priority; ViewBag.Category = category;
        return View(list);
    }

    // ---- Resident: my complaints ----
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> MyComplaints()
    {
        var uid = _um.GetUserId(User);
        var list = await _db.Complaints
            .Include(c => c.Flat)
            .Include(c => c.AssignedStaff)
            .Where(c => c.RaisedByUserId == uid)
            .OrderByDescending(c => c.CreatedAt).ToListAsync();
        return View(list);
    }

    // ---- Resident: create ----
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Create()
    {
        await LoadCategoriesAsync();
        return View(new Complaint());
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Create(Complaint vm)
    {
        ModelState.Remove(nameof(Complaint.Flat));
        ModelState.Remove(nameof(Complaint.RaisedBy));
        ModelState.Remove(nameof(Complaint.AssignedStaff));

        var uid = _um.GetUserId(User)!;
        var profile = await _db.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == uid && r.IsActive);
        if (profile == null)
        {
            ModelState.AddModelError("", "Your account is not linked to a flat.");
            return View(vm);
        }

        if (string.IsNullOrWhiteSpace(vm.Title) || string.IsNullOrWhiteSpace(vm.Description))
            ModelState.AddModelError("", "Title and Description are required.");

        if (!ModelState.IsValid) return View(vm);

        vm.FlatId = profile.FlatId;
        vm.RaisedByUserId = uid;
        vm.Status = ComplaintStatus.OPEN;
        vm.CreatedAt = DateTime.UtcNow;
        _db.Complaints.Add(vm);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Complaint submitted. The society will review it shortly.";
        return RedirectToAction(nameof(MyComplaints));
    }

    // ---- Details (both roles) ----
    public async Task<IActionResult> Details(int id)
    {
        var c = await _db.Complaints
            .Include(x => x.Flat).ThenInclude(f => f.Building)
            .Include(x => x.RaisedBy)
            .Include(x => x.AssignedStaff)
            .FirstOrDefaultAsync(x => x.Id == id);
        if (c == null) return NotFound();

        // Resident can only see own
        if (User.IsInRole("Resident") && c.RaisedByUserId != _um.GetUserId(User))
            return Forbid();

        if (User.IsInRole("Admin"))
        {
            ViewBag.Staff = new SelectList(await _db.Staffs.Where(s => s.IsActive).OrderBy(s => s.Name).ToListAsync(), "Id", "Name", c.AssignedStaffId);
            ViewBag.Statuses = Enum.GetValues<ComplaintStatus>();
        }
        return View(c);
    }

    // ---- Admin actions ----
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Assign(int id, int? staffId, ComplaintStatus status)
    {
        var c = await _db.Complaints.FindAsync(id);
        if (c == null) return NotFound();

        if (staffId.HasValue) { c.AssignedStaffId = staffId; if (c.Status == ComplaintStatus.OPEN) c.Status = ComplaintStatus.ASSIGNED; }
        c.Status = status;

        if (c.Status == ComplaintStatus.RESOLVED && c.ResolvedAt == null) c.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Complaint updated.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Resolve(int id, string? resolutionNotes)
    {
        var c = await _db.Complaints.FindAsync(id);
        if (c == null) return NotFound();
        c.ResolutionNotes = resolutionNotes;
        c.Status = ComplaintStatus.RESOLVED;
        c.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Complaint marked as resolved.";
        return RedirectToAction(nameof(Details), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Close(int id)
    {
        var c = await _db.Complaints.FindAsync(id);
        if (c == null) return NotFound();
        c.Status = ComplaintStatus.CLOSED;
        if (c.ResolvedAt == null) c.ResolvedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Complaint closed.";
        return RedirectToAction(nameof(Details), new { id });
    }

    // ---- Resident: add info ----
    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> AddInfo(int id, string note)
    {
        var c = await _db.Complaints.FindAsync(id);
        if (c == null) return NotFound();
        if (c.RaisedByUserId != _um.GetUserId(User)) return Forbid();
        c.Description += "\n\n--- Additional info ---\n" + note;
        await _db.SaveChangesAsync();
        TempData["Success"] = "Additional information added.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task LoadCategoriesAsync()
    {
        await Task.CompletedTask;
    }
}