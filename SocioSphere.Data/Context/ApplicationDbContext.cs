using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using SocioSphere.Core.Entities;

namespace SocioSphere.Data.Context;

public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

    public DbSet<Society> Societies => Set<Society>();
    public DbSet<Building> Buildings => Set<Building>();
    public DbSet<Flat> Flats => Set<Flat>();
    public DbSet<ResidentProfile> ResidentProfiles => Set<ResidentProfile>();
    public DbSet<MaintenanceBill> MaintenanceBills => Set<MaintenanceBill>();
    public DbSet<Payment> Payments => Set<Payment>();
    public DbSet<Complaint> Complaints => Set<Complaint>();
    public DbSet<Notice> Notices => Set<Notice>();
    public DbSet<Amenity> Amenities => Set<Amenity>();
    public DbSet<AmenityBooking> AmenityBookings => Set<AmenityBooking>();
    public DbSet<Visitor> Visitors => Set<Visitor>();
    public DbSet<ExpectedVisitor> ExpectedVisitors => Set<ExpectedVisitor>();
    public DbSet<ParkingSlot> ParkingSlots => Set<ParkingSlot>();
    public DbSet<Vehicle> Vehicles => Set<Vehicle>();
    public DbSet<SocietyExpense> SocietyExpenses => Set<SocietyExpense>();
    public DbSet<Vendor> Vendors => Set<Vendor>();
    public DbSet<Staff> Staffs => Set<Staff>();
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();

    protected override void OnModelCreating(ModelBuilder b)
    {
        base.OnModelCreating(b);

        b.Entity<Flat>().HasIndex(f => new { f.BuildingId, f.FlatNumber }).IsUnique();
        b.Entity<MaintenanceBill>().HasIndex(m => new { m.FlatId, m.BillingMonth }).IsUnique();
        b.Entity<ParkingSlot>().HasIndex(p => p.SlotNumber).IsUnique();
        b.Entity<Vehicle>().HasIndex(v => v.RegistrationNumber).IsUnique().HasFilter("[IsActive] = 1");

        foreach (var e in b.Model.GetEntityTypes())
            foreach (var p in e.GetProperties())
                if (p.ClrType == typeof(decimal) || p.ClrType == typeof(decimal?))
                    p.SetColumnType("decimal(18,2)");

        b.Entity<ResidentProfile>()
            .HasOne(r => r.User).WithOne(u => u.ResidentProfile)
            .HasForeignKey<ResidentProfile>(r => r.UserId).OnDelete(DeleteBehavior.Cascade);

        b.Entity<Complaint>()
            .HasOne(c => c.RaisedBy).WithMany()
            .HasForeignKey(c => c.RaisedByUserId).OnDelete(DeleteBehavior.Restrict);
    }
}
