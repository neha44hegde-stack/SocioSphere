using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class SocietyExpense
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public ExpenseCategory Category { get; set; }
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public string? Description { get; set; }
    public int? VendorId { get; set; }
    public Vendor? Vendor { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
