using SocioSphere.Core.Enums;

namespace SocioSphere.Core.Entities;

public class Notice
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Content { get; set; } = "";
    public NoticeCategory Category { get; set; } = NoticeCategory.GENERAL;
    public NoticePriority Priority { get; set; } = NoticePriority.NORMAL;
    public DateTime PublishedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
    public string CreatedByUserId { get; set; } = "";
}
