using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Payment
{
    public int Id { get; set; }
    public int MaintenanceBillId { get; set; }
    public MaintenanceBill MaintenanceBill { get; set; } = null!;
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    public PaymentMethod Method { get; set; }
    public string? TransactionReference { get; set; }
    public PaymentStatus Status { get; set; } = PaymentStatus.SUCCESS;
    public string? RecordedByUserId { get; set; }
}
