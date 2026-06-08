using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Entities;

public class StatusHistory : BaseEntity
{
    public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public ComplaintStatus? FromStatus { get; set; }
    public ComplaintStatus ToStatus { get; set; }
    public string Note { get; set; } = string.Empty;
    public int ChangedById { get; set; }
    public ApplicationUser ChangedBy { get; set; } = null!;
}
