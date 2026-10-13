using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Flat
{
    public int Id { get; set; }
    public int BuildingId { get; set; }
    public Building Building { get; set; } = null!;
    public string FlatNumber { get; set; } = "";
    public int Floor { get; set; }
    public string FlatType { get; set; } = "";
    public decimal Area { get; set; }
    public FlatStatus Status { get; set; } = FlatStatus.VACANT;

    public ICollection<ResidentProfile> Residents { get; set; } = new List<ResidentProfile>();
    public ICollection<MaintenanceBill> Bills { get; set; } = new List<MaintenanceBill>();
    public ICollection<Vehicle> Vehicles { get; set; } = new List<Vehicle>();
}
