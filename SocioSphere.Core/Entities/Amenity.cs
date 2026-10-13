namespace SocioSphere.Core.Entities;

public class Amenity
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Description { get; set; }
    public int Capacity { get; set; }
    public TimeSpan OpeningTime { get; set; }
    public TimeSpan ClosingTime { get; set; }
    public int MaxBookingMinutes { get; set; } = 120;
    public bool IsActive { get; set; } = true;

    public ICollection<AmenityBooking> Bookings { get; set; } = new List<AmenityBooking>();
}
