using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Visitor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public int FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public string? Purpose { get; set; }
    public string? VehicleNumber { get; set; }
    public VisitorType Type { get; set; } = VisitorType.GUEST;
    public VisitorStatus Status { get; set; } = VisitorStatus.CHECKED_IN;
    public DateTime EntryTime { get; set; } = DateTime.UtcNow;
    public DateTime? ExitTime { get; set; }
    public string? RegisteredByUserId { get; set; }
}
