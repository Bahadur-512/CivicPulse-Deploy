namespace CivicPulse.Core.DTOs;

public class AdminDashboardStatsDto
{
    public int TotalComplaints { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public int SlaBreachedCount { get; set; }
    public double AverageResolutionDays { get; set; }
    public double SlaCompliancePercent { get; set; }
}

public class CitizenDashboardDto
{
    public int TotalComplaints { get; set; }
    public int PendingCount { get; set; }
    public int InProgressCount { get; set; }
    public int ResolvedCount { get; set; }
    public List<ComplaintDto> RecentComplaints { get; set; } = new();
    public int UnreadNotificationCount { get; set; }
}

public class CategoryBreakdownDto
{
    public string CategoryName { get; set; } = string.Empty;
    public int Count { get; set; }
    public string ColorHex { get; set; } = string.Empty;
}

public class AreaHeatmapDto
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public string Priority { get; set; } = string.Empty;
    public string ComplaintNumber { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
}

public class MonthlyTrendDto
{
    public string Month { get; set; } = string.Empty;
    public int Year { get; set; }
    public int Count { get; set; }
}

public class ResolutionTimeDto
{
    public string Month { get; set; } = string.Empty;
    public double AvgDays { get; set; }
}

public class PriorityDistributionDto
{
    public string Priority { get; set; } = string.Empty;
    public int Count { get; set; }
    public string Color { get; set; } = string.Empty;
}

public class SlaComplianceDto
{
    public double ComplianceRate { get; set; }
    public int BreachedCount { get; set; }
    public int TotalTracked { get; set; }
}

public class CategoryDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string IconName { get; set; } = string.Empty;
    public string ColorHex { get; set; } = string.Empty;
    public int DefaultSlaDays { get; set; }
    public string DefaultPriority { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public string Keywords { get; set; } = string.Empty;
}

public class NotificationDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public int? RelatedComplaintId { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AuditLogDto
{
    public int Id { get; set; }
    public string Action { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public int? EntityId { get; set; }
    public string OldValues { get; set; } = string.Empty;
    public string NewValues { get; set; } = string.Empty;
    public string IpAddress { get; set; } = string.Empty;
    public int? UserId { get; set; }
    public string UserName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

public class AuditLogFilterDto
{
    public string? Action { get; set; }
    public string? EntityType { get; set; }
    public int? UserId { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}

public class CategorizationResult
{
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public double Confidence { get; set; }
    public bool IsConfident { get; set; }
}

