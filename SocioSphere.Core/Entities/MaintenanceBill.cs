using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class MaintenanceBill
{
    public int Id { get; set; }
    public int FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public string BillingMonth { get; set; } = "";
    public decimal BaseMaintenance { get; set; }
    public decimal WaterCharge { get; set; }
    public decimal ParkingCharge { get; set; }
    public decimal OtherCharge { get; set; }
    public decimal LateFee { get; set; }
    public decimal TotalAmount { get; set; }
    public DateTime DueDate { get; set; }
    public BillStatus Status { get; set; } = BillStatus.PENDING;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
}
