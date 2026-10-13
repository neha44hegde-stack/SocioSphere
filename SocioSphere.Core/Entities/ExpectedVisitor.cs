namespace SocioSphere.Core.Entities;

public class ExpectedVisitor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public int FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public DateTime ExpectedDate { get; set; }
    public TimeSpan? ExpectedTime { get; set; }
    public string? Purpose { get; set; }
    public string? VehicleNumber { get; set; }
    public string CreatedByUserId { get; set; } = "";
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
