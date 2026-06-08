using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Entities;

public class Complaint : BaseEntity
{
    public string ComplaintNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; } = ComplaintStatus.Pending;
    public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
    public bool IsAiCategorized { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string LocationDescription { get; set; } = string.Empty;
    public DateTime? SlaDeadline { get; set; }
    public bool IsSlaBreached { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime? ReopenedAt { get; set; }
    public string ReopenReason { get; set; } = string.Empty;
    public int ReopenCount { get; set; }
    public bool IsArchived { get; set; }
    public bool FeedbackRequested { get; set; }
    public bool SlaWarningEmailSent { get; set; } = false;

    public int CitizenId { get; set; }
    public ApplicationUser Citizen { get; set; } = null!;
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    public int? AssignedAdminId { get; set; }
    public ApplicationUser? AssignedAdmin { get; set; }

    public ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public Feedback? Feedback { get; set; }
    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
}
