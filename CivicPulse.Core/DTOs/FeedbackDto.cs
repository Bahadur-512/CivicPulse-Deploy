namespace CivicPulse.Core.DTOs;

public class FeedbackDto
{
    public int Id { get; set; }
    public int ComplaintId { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public int CitizenId { get; set; }
    public string CitizenName { get; set; } = string.Empty;
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool WasResolutionSatisfactory { get; set; }
    public bool WasResponseTimely { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class CreateFeedbackDto
{
    public int ComplaintId { get; set; }
    public int CitizenId { get; set; }
    public int Rating { get; set; }
    public string Comment { get; set; } = string.Empty;
    public bool WasResolutionSatisfactory { get; set; }
    public bool WasResponseTimely { get; set; }
}
