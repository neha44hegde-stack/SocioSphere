using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Vehicle
{
    public int Id { get; set; }
    public string RegistrationNumber { get; set; } = "";
    public ParkingType Type { get; set; }
    public string? Model { get; set; }
    public string? Color { get; set; }
    public int FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public bool IsActive { get; set; } = true;
}
