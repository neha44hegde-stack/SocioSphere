using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class ParkingSlot
{
    public int Id { get; set; }
    public string SlotNumber { get; set; } = "";
    public ParkingType Type { get; set; }
    public ParkingStatus Status { get; set; } = ParkingStatus.AVAILABLE;
    public int? FlatId { get; set; }
    public Flat? Flat { get; set; }
    public string? VehicleNumber { get; set; }
}
