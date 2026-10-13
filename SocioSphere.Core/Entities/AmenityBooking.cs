using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class AmenityBooking
{
    public int Id { get; set; }
    public int AmenityId { get; set; }
    public Amenity Amenity { get; set; } = null!;
    public string ResidentUserId { get; set; } = "";
    public ApplicationUser Resident { get; set; } = null!;
    public DateTime BookingDate { get; set; }
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }
    public string? Purpose { get; set; }
    public BookingStatus Status { get; set; } = BookingStatus.CONFIRMED;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
