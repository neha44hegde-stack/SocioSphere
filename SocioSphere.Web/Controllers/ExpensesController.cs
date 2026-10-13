using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize(Roles = "Admin")]
public class ExpensesController : Controller
{
    private readonly ApplicationDbContext _db;
    public ExpensesController(ApplicationDbContext db) { _db = db; }

    public async Task<IActionResult> Index(ExpenseCategory? category)
    {
        var q = _db.SocietyExpenses.Include(e => e.Vendor).AsQueryable();
        if (category.HasValue) q = q.Where(e => e.Category == category.Value);
        var list = await q.OrderByDescending(e => e.ExpenseDate).Take(200).ToListAsync();
        ViewBag.Category = category;
        ViewBag.Total = list.Sum(e => e.Amount);
        ViewBag.Vendors = new Microsoft.AspNetCore.Mvc.Rendering.SelectList(
            await _db.Vendors.OrderBy(v => v.Name).ToListAsync(), "Id", "Name");
        return View(list);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string name, ExpenseCategory category, decimal amount, DateTime expenseDate, string? description, int? vendorId)
    {
        if (string.IsNullOrWhiteSpace(name) || amount <= 0) { TempData["Error"] = "Name and positive amount required."; return RedirectToAction(nameof(Index)); }
        _db.SocietyExpenses.Add(new SocietyExpense { Name = name, Category = category, Amount = amount, ExpenseDate = expenseDate, Description = description, VendorId = vendorId });
        await _db.SaveChangesAsync();
        TempData["Success"] = "Expense recorded.";
        return RedirectToAction(nameof(Index));
    }
}