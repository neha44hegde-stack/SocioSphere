using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Vendor
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string? Service { get; set; }
    public string? ContactPerson { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public DateTime? ContractStart { get; set; }
    public DateTime? ContractEnd { get; set; }
    public VendorStatus Status { get; set; } = VendorStatus.ACTIVE;
}
