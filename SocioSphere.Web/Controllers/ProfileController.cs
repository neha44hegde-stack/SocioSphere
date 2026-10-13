using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;
using SocioSphere.Data.Context;

namespace SocioSphere.Web.Controllers;

[Authorize]
public class ProfileController : Controller
{
    private readonly UserManager<ApplicationUser> _um;
    private readonly ApplicationDbContext _db;
    public ProfileController(UserManager<ApplicationUser> um, ApplicationDbContext db) { _um = um; _db = db; }

    public async Task<IActionResult> Index()
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");

        var roles = await _um.GetRolesAsync(user);
        ViewBag.Roles = roles;
        ViewBag.FlatNumber = null;
        ViewBag.BuildingName = null;
        ViewBag.Relationship = (object)null;
        ViewBag.IsPrimary = false;

        var rp = await _db.ResidentProfiles.Include(r => r.Flat).ThenInclude(f => f.Building)
            .FirstOrDefaultAsync(r => r.UserId == user.Id && r.IsActive);
        if (rp != null)
        {
            ViewBag.FlatNumber = rp.Flat.FlatNumber;
            ViewBag.BuildingName = rp.Flat.Building.Name;
            ViewBag.Relationship = rp.Relationship.ToString();
            ViewBag.IsPrimary = rp.IsPrimaryResident;
        }

        return View(user);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(string name, string? phone)
    {
        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");
        user.Name = name ?? user.Name;
        user.Phone = phone;
        await _um.UpdateAsync(user);
        TempData["Success"] = "Profile updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        if (newPassword != confirmPassword) { TempData["Error"] = "New passwords do not match."; return RedirectToAction(nameof(Index)); }
        if (newPassword.Length < 8) { TempData["Error"] = "Password must be at least 8 characters."; return RedirectToAction(nameof(Index)); }

        var user = await _um.GetUserAsync(User);
        if (user == null) return Redirect("/Identity/Account/Login");

        var r = await _um.ChangePasswordAsync(user, currentPassword, newPassword);
        if (r.Succeeded) TempData["Success"] = "Password changed.";
        else TempData["Error"] = string.Join("; ", r.Errors.Select(e => e.Description));
        return RedirectToAction(nameof(Index));
    }
}