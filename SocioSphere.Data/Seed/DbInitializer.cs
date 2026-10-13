using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using SocioSphere.Core.Entities;
using SocioSphere.Core.Enums;
using SocioSphere.Data.Context;

namespace SocioSphere.Data.Seed;

public static class DbInitializer
{
    public static async Task SeedAsync(IServiceProvider sp)
    {
        var db = sp.GetRequiredService<ApplicationDbContext>();
        var users = sp.GetRequiredService<UserManager<ApplicationUser>>();
        var roles = sp.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var r in new[] { "Admin", "Resident", "Security" })
            if (!await roles.RoleExistsAsync(r)) await roles.CreateAsync(new IdentityRole(r));

                // Ensure seeded users have their roles (idempotent)
        foreach (var pair in new[] { ("admin@sociosphere.local", "Admin"), ("resident@sociosphere.local", "Resident"), ("security@sociosphere.local", "Security") })
        {
            var u = await users.FindByEmailAsync(pair.Item1);
            if (u != null && !await users.IsInRoleAsync(u, pair.Item2))
                await users.AddToRoleAsync(u, pair.Item2);
        }

        if (await db.Societies.AnyAsync()) return;

        var s = new Society { Name = "Green Valley Residency", Address = "MG Road", City = "Bengaluru" };
        db.Societies.Add(s); await db.SaveChangesAsync();

        var a = new Building { SocietyId = s.Id, Name = "A", TotalFloors = 4 };
        var b = new Building { SocietyId = s.Id, Name = "B", TotalFloors = 4 };
        db.Buildings.AddRange(a, b); await db.SaveChangesAsync();

        var flats = new[] {
            new Flat { BuildingId = a.Id, FlatNumber = "A-101", Floor = 1, FlatType = "2BHK", Area = 950, Status = FlatStatus.OCCUPIED },
            new Flat { BuildingId = a.Id, FlatNumber = "A-102", Floor = 1, FlatType = "2BHK", Area = 950 },
            new Flat { BuildingId = a.Id, FlatNumber = "A-201", Floor = 2, FlatType = "3BHK", Area = 1250 },
            new Flat { BuildingId = b.Id, FlatNumber = "B-101", Floor = 1, FlatType = "2BHK", Area = 950 },
            new Flat { BuildingId = b.Id, FlatNumber = "B-102", Floor = 1, FlatType = "3BHK", Area = 1250 }
        };
        db.Flats.AddRange(flats); await db.SaveChangesAsync();

        var admin = await MkUser(users, "admin@sociosphere.local", "Society Admin", "Admin", "9000000001");
        var res   = await MkUser(users, "resident@sociosphere.local", "Rahul Sharma", "Resident", "9000000002");
        var sec   = await MkUser(users, "security@sociosphere.local", "Guard Ramesh", "Security", "9000000003");

        var a101 = flats[0];
        db.ResidentProfiles.Add(new ResidentProfile { UserId = res.Id, FlatId = a101.Id, Relationship = RelationshipType.OWNER, IsPrimaryResident = true });
        db.Staffs.Add(new Staff { Name = "Guard Ramesh", Role = StaffRole.SECURITY_GUARD, Phone = "9000000003", Shift = "Day", JoiningDate = DateTime.UtcNow, UserId = sec.Id });

        db.Amenities.AddRange(
            new Amenity { Name = "Clubhouse", Capacity = 50, OpeningTime = TimeSpan.FromHours(9), ClosingTime = TimeSpan.FromHours(22) },
            new Amenity { Name = "Gym", Capacity = 20, OpeningTime = TimeSpan.FromHours(6), ClosingTime = TimeSpan.FromHours(22), MaxBookingMinutes = 60 },
            new Amenity { Name = "Swimming Pool", Capacity = 30, OpeningTime = TimeSpan.FromHours(6), ClosingTime = TimeSpan.FromHours(21) }
        );

        for (int i = 1; i <= 5; i++)
            db.ParkingSlots.Add(new ParkingSlot { SlotNumber = $"P-{i:D2}", Type = ParkingType.FOUR_WHEELER, Status = ParkingStatus.AVAILABLE });

        db.Notices.Add(new Notice { Title = "Welcome to Green Valley Residency", Content = "Please update your profile.", Category = NoticeCategory.GENERAL, CreatedByUserId = admin.Id });

        db.MaintenanceBills.Add(new MaintenanceBill {
            FlatId = a101.Id,
            BillingMonth = DateTime.UtcNow.ToString("yyyy-MM"),
            BaseMaintenance = 2000m, WaterCharge = 300m, ParkingCharge = 500m,
            TotalAmount = 2800m,
            DueDate = DateTime.UtcNow.AddDays(15),
            Status = BillStatus.PENDING
        });

        await db.SaveChangesAsync();
    }

    static async Task<ApplicationUser> MkUser(UserManager<ApplicationUser> um, string email, string name, string role, string phone)
    {
        var u = new ApplicationUser { UserName = email, Email = email, EmailConfirmed = true, Name = name, Phone = phone };
        await um.CreateAsync(u, "Pass@123");
        await um.AddToRoleAsync(u, role);
        return u;
    }
}

