namespace SocioSphere.Core.Entities;

public class Building
{
    public int Id { get; set; }
    public int SocietyId { get; set; }
    public Society Society { get; set; } = null!;
    public string Name { get; set; } = "";
    public int TotalFloors { get; set; }

    public ICollection<Flat> Flats { get; set; } = new List<Flat>();
}
