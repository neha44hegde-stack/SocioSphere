namespace SocioSphere.Core.Entities;

public class Society
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? RegistrationNumber { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public ICollection<Building> Buildings { get; set; } = new List<Building>();
}
