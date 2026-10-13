using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ReportsController : Controller
{
    private readonly ApplicationDbContext _db;
    public ReportsController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index()
    {
        var bills = await _db.MaintenanceBills.ToListAsync();
        var pays = await _db.Payments.Where(p => p.Status == PaymentStatus.SUCCESS).ToListAsync();
        var expenses = await _db.SocietyExpenses.ToListAsync();

        ViewBag.TotalBilled = bills.Sum(b => b.TotalAmount);
        ViewBag.TotalCollected = pays.Sum(p => p.Amount);
        ViewBag.TotalExpenses = expenses.Sum(e => e.Amount);
        ViewBag.Outstanding = ViewBag.TotalBilled - ViewBag.TotalCollected;
        ViewBag.BillCount = bills.Count;
        ViewBag.PaymentCount = pays.Count;
        ViewBag.ExpenseCount = expenses.Count;

        return View();
    }
}