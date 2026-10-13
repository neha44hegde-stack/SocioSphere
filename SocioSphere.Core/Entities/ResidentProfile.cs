using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class ResidentProfile
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser User { get; set; } = null!;
    public int FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public RelationshipType Relationship { get; set; }
    public bool IsPrimaryResident { get; set; }
    public bool IsActive { get; set; } = true;
    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
}
