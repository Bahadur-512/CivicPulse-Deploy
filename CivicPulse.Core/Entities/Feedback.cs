namespace CivicPulse.Core.Entities;

public class Feedback : BaseEntity
{
    public int ComplaintId { get; set; }
    public Complaint Complaint { get; set; } = null!;
    public int CitizenId { get; set; }
    public ApplicationUser Citizen { get; set; } = null!;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool WasResolutionSatisfactory { get; set; }
    public bool WasResponseTimely { get; set; }
}
