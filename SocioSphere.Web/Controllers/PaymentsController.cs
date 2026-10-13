using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

public class PaymentsController : Controller
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<ApplicationUser> _um;
    public PaymentsController(ApplicationDbContext db, UserManager<ApplicationUser> um) { _db = db; _um = um; }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Index(DateTime? from, DateTime? to, int? flatId, PaymentStatus? status, PaymentMethod? method)
    {
        var q = _db.Payments
            .Include(p => p.MaintenanceBill).ThenInclude(b => b.Flat).ThenInclude(f => f.Building)
            .AsQueryable();

        if (from.HasValue) q = q.Where(p => p.PaymentDate >= from.Value);
        if (to.HasValue) q = q.Where(p => p.PaymentDate <= to.Value.Date.AddDays(1));
        if (flatId.HasValue) q = q.Where(p => p.MaintenanceBill.FlatId == flatId.Value);
        if (status.HasValue) q = q.Where(p => p.Status == status.Value);
        if (method.HasValue) q = q.Where(p => p.Method == method.Value);

        var list = await q.OrderByDescending(p => p.PaymentDate).Take(200).ToListAsync();

        ViewBag.From = from; ViewBag.To = to; ViewBag.FlatId = flatId;
        ViewBag.Status = status; ViewBag.Method = method;

        var flats = await _db.Flats.Include(f => f.Building).OrderBy(f => f.FlatNumber).ToListAsync();
        ViewBag.Flats = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(flats.Select(f => new { f.Id, Display = f.FlatNumber + " (" + f.Building.Name + ")" }), "Id", "Display");
        return View(list);
    }

    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Record(int billId)
    {
        var bill = await _db.MaintenanceBills
            .Include(b => b.Flat)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == billId);
        if (bill == null) return NotFound();

        var paid = bill.Payments.Where(p => p.Status == PaymentStatus.SUCCESS).Sum(p => p.Amount);
        ViewBag.Bill = bill;
        ViewBag.Paid = paid;
        ViewBag.Outstanding = bill.TotalAmount - paid;

        return View(new Payment
        {
            MaintenanceBillId = billId,
            Amount = bill.TotalAmount - paid,
            PaymentDate = DateTime.UtcNow,
            Status = PaymentStatus.SUCCESS,
            Method = PaymentMethod.UPI
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Record(Payment vm)
    {
        ModelState.Remove(nameof(Payment.MaintenanceBill));

        var bill = await _db.MaintenanceBills.Include(b => b.Payments).FirstOrDefaultAsync(b => b.Id == vm.MaintenanceBillId);
        if (bill == null) return NotFound();

        var paid = bill.Payments.Where(p => p.Status == PaymentStatus.SUCCESS).Sum(p => p.Amount);
        var outstanding = bill.TotalAmount - paid;

        if (vm.Amount <= 0)
            ModelState.AddModelError(nameof(vm.Amount), "Amount must be greater than zero.");
        if (vm.Amount > outstanding)
            ModelState.AddModelError(nameof(vm.Amount), $"Amount cannot exceed outstanding balance of ₹{outstanding:N0}.");

        if (!ModelState.IsValid)
        {
            ViewBag.Bill = bill; ViewBag.Paid = paid; ViewBag.Outstanding = outstanding;
            return View(vm);
        }

        vm.PaymentDate = DateTime.UtcNow;
        vm.RecordedByUserId = _um.GetUserId(User);
        _db.Payments.Add(vm);
        await _db.SaveChangesAsync();

        await UpdateBillStatus(bill.Id);
        TempData["Success"] = $"Payment of ₹{vm.Amount:N0} recorded.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var p = await _db.Payments.FindAsync(id);
        if (p == null) return NotFound();
        var billId = p.MaintenanceBillId;
        _db.Payments.Remove(p);
        await _db.SaveChangesAsync();
        await UpdateBillStatus(billId);
        TempData["Success"] = "Payment deleted.";
        return RedirectToAction(nameof(Index));
    }

    
    // ---------- Resident payment flow ----------
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Pay(int billId)
    {
        var bill = await _db.MaintenanceBills
            .Include(b => b.Flat).ThenInclude(f => f.Building)
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == billId);
        if (bill == null) return NotFound();

        var uid = _um.GetUserId(User);
        var profile = await _db.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == uid && r.IsActive);
        if (profile == null || profile.FlatId != bill.FlatId) return Forbid();

        var paid = bill.Payments.Where(p => p.Status == PaymentStatus.SUCCESS).Sum(p => p.Amount);
        ViewBag.Bill = bill;
        ViewBag.Paid = paid;
        ViewBag.Outstanding = bill.TotalAmount - paid;

        return View(new Payment
        {
            MaintenanceBillId = billId,
            Amount = bill.TotalAmount - paid,
            PaymentDate = DateTime.UtcNow,
            Status = PaymentStatus.SUCCESS,
            Method = PaymentMethod.UPI
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    [Authorize(Roles = "Resident")]
    public async Task<IActionResult> Pay(Payment vm)
    {
        ModelState.Remove(nameof(Payment.MaintenanceBill));

        var bill = await _db.MaintenanceBills
            .Include(b => b.Payments)
            .FirstOrDefaultAsync(b => b.Id == vm.MaintenanceBillId);
        if (bill == null) return NotFound();

        var uid = _um.GetUserId(User);
        var profile = await _db.ResidentProfiles.FirstOrDefaultAsync(r => r.UserId == uid && r.IsActive);
        if (profile == null || profile.FlatId != bill.FlatId) return Forbid();

        var paid = bill.Payments.Where(p => p.Status == PaymentStatus.SUCCESS).Sum(p => p.Amount);
        var outstanding = bill.TotalAmount - paid;

        if (vm.Amount <= 0) ModelState.AddModelError(nameof(vm.Amount), "Amount must be > 0.");
        if (vm.Amount > outstanding) ModelState.AddModelError(nameof(vm.Amount), $"Cannot exceed outstanding.");

        if (!ModelState.IsValid)
        {
            ViewBag.Bill = bill; ViewBag.Paid = paid; ViewBag.Outstanding = outstanding;
            return View(vm);
        }

        vm.PaymentDate = DateTime.UtcNow;
        vm.Status = PaymentStatus.SUCCESS;
        vm.RecordedByUserId = _um.GetUserId(User);
        _db.Payments.Add(vm);
        await _db.SaveChangesAsync();
        await UpdateBillStatus(bill.Id);

        TempData["Success"] = $"Payment recorded.";
        return RedirectToAction("MyPayments", "Resident");
    }

    private async Task UpdateBillStatus(int billId)
    {
        var bill = await _db.MaintenanceBills.Include(b => b.Payments).FirstAsync(b => b.Id == billId);
        var paid = bill.Payments.Where(p => p.Status == PaymentStatus.SUCCESS).Sum(p => p.Amount);

        if (paid <= 0)
            bill.Status = bill.DueDate < DateTime.UtcNow.Date ? BillStatus.OVERDUE : BillStatus.PENDING;
        else if (paid >= bill.TotalAmount)
            bill.Status = BillStatus.PAID;
        else
            bill.Status = BillStatus.PARTIALLY_PAID;

        await _db.SaveChangesAsync();
    }
}




