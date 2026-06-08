using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Entities;

public class Notification : BaseEntity
{
    public int UserId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public string Title { get; set; } = string.Empty;
    public string TitleUrdu { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string BodyUrdu { get; set; } = string.Empty;
    public NotificationType Type { get; set; }
    public bool IsRead { get; set; }
    public int? RelatedComplaintId { get; set; }
    public Complaint? RelatedComplaint { get; set; }
}
