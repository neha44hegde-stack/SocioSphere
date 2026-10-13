using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Complaint
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public ComplaintCategory Category { get; set; }
    public ComplaintPriority Priority { get; set; } = ComplaintPriority.MEDIUM;
    public int FlatId { get; set; }
    public Flat Flat { get; set; } = null!;
    public string RaisedByUserId { get; set; } = "";
    public ApplicationUser RaisedBy { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.OPEN;
    public int? AssignedStaffId { get; set; }
    public Staff? AssignedStaff { get; set; }
    public string? ResolutionNotes { get; set; }
    public DateTime? ResolvedAt { get; set; }
}
