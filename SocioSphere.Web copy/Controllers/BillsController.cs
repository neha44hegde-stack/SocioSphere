using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class BillsController : Controller
{
    private readonly ApplicationDbContext _db;
    public BillsController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index(string? month, int? flatId, BillStatus? status)
    {
        var q = _db.MaintenanceBills
            .Include(b => b.Flat).ThenInclude(f => f.Building)
            .Include(b => b.Payments)
            .AsQueryable();

        if (!string.IsNullOrEmpty(month)) q = q.Where(b => b.BillingMonth == month);
        if (flatId.HasValue) q = q.Where(b => b.FlatId == flatId.Value);
        if (status.HasValue) q = q.Where(b => b.Status == status.Value);

        var bills = await q.OrderByDescending(b => b.BillingMonth).ThenBy(b => b.Flat.FlatNumber).ToListAsync();

        // Refresh overdue statuses
        foreach (var b in bills)
            if (b.Status != BillStatus.PAID && b.DueDate < DateTime.UtcNow.Date)
                b.Status = BillStatus.OVERDUE;
        await _db.SaveChangesAsync();

        ViewBag.Month = month;
        ViewBag.FlatId = flatId;
        ViewBag.Status = status;
        await LoadFlats();
        return View(bills);
    }

    public async Task<IActionResult> Create()
    {
        await LoadFlats();
        return View(new MaintenanceBill
        {
            BillingMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            DueDate = DateTime.UtcNow.AddDays(15)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(MaintenanceBill vm)
    {
        ModelState.Remove(nameof(MaintenanceBill.Flat));
        ModelState.Remove(nameof(MaintenanceBill.Payments));

        if (!ModelState.IsValid) { await LoadFlats(); return View(vm); }

        if (await _db.MaintenanceBills.AnyAsync(b => b.FlatId == vm.FlatId && b.BillingMonth == vm.BillingMonth))
        {
            ModelState.AddModelError("", $"A bill already exists for this flat in {vm.BillingMonth}.");
            await LoadFlats();
            return View(vm);
        }

        vm.TotalAmount = vm.BaseMaintenance + vm.WaterCharge + vm.ParkingCharge + vm.OtherCharge + vm.LateFee;
        vm.Status = BillStatus.PENDING;
        vm.CreatedAt = DateTime.UtcNow;
        _db.MaintenanceBills.Add(vm);
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Bill created for {vm.BillingMonth}.";
        return RedirectToAction(nameof(Index));
    }

    public IActionResult Generate() => View();

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Generate(string billingMonth, decimal baseAmt, decimal water, decimal parking, decimal other, int dueDays)
    {
        if (string.IsNullOrEmpty(billingMonth))
        {
            TempData["Error"] = "Billing month is required.";
            return RedirectToAction(nameof(Generate));
        }

        var occupied = await _db.Flats.Where(f => f.Status == FlatStatus.OCCUPIED).ToListAsync();
        int created = 0, skipped = 0;

        foreach (var flat in occupied)
        {
            if (await _db.MaintenanceBills.AnyAsync(b => b.FlatId == flat.Id && b.BillingMonth == billingMonth))
            { skipped++; continue; }

            _db.MaintenanceBills.Add(new MaintenanceBill
            {
                FlatId = flat.Id,
                BillingMonth = billingMonth,
                BaseMaintenance = baseAmt,
                WaterCharge = water,
                ParkingCharge = parking,
                OtherCharge = other,
                LateFee = 0,
                TotalAmount = baseAmt + water + parking + other,
                DueDate = DateTime.UtcNow.AddDays(dueDays <= 0 ? 15 : dueDays),
                Status = BillStatus.PENDING
            });
            created++;
        }
        await _db.SaveChangesAsync();
        TempData["Success"] = $"Generated {created} bill(s). {skipped} skipped (already existed).";
        return RedirectToAction(nameof(Index));
    }

    public async Task<IActionResult> Details(int id)
    {
        var bill = await _db.MaintenanceBills
            .Include(b => b.Flat).ThenInclude(f => f.Building)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == id);
        if (bill == null) return NotFound();

        ViewBag.Paid = bill.Payments.Where(p => p.Status == PaymentStatus.SUCCESS).Sum(p => p.Amount);
        ViewBag.Outstanding = bill.TotalAmount - (decimal)ViewBag.Paid;
        return View(bill);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var bill = await _db.MaintenanceBills.Include(b => b.Payments).FirstOrDefaultAsync(b => b.Id == id);
        if (bill == null) return NotFound();
        if (bill.Payments.Any())
        {
            TempData["Error"] = "Cannot delete a bill with recorded payments.";
            return RedirectToAction(nameof(Index));
        }
        _db.MaintenanceBills.Remove(bill);
        await _db.SaveChangesAsync();
        TempData["Success"] = "Bill deleted.";
        return RedirectToAction(nameof(Index));
    }

    private async Task LoadFlats()
    {
        var flats = await _db.Flats.Include(f => f.Building).OrderBy(f => f.FlatNumber).ToListAsync();
        ViewBag.Flats = new SelectList(flats.Select(f => new { f.Id, Display = f.FlatNumber + " (" + f.Building.Name + ")" }), "Id", "Display");
    }
}
