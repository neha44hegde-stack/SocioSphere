using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Staff
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public StaffRole Role { get; set; }
    public string? Phone { get; set; }
    public string? Shift { get; set; }
    public DateTime? JoiningDate { get; set; }
    public bool IsActive { get; set; } = true;
    public string? UserId { get; set; }
}
