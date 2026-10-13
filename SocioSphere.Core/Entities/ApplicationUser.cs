using Microsoft.AspNetCore.Identity;

namespace SocioSphere.Core.Entities;

public class ApplicationUser : IdentityUser
{
    public string Name { get; set; } = "";
    public string? Phone { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ResidentProfile? ResidentProfile { get; set; }
}
