using CivicPulse.Core.Enums;

namespace CivicPulse.Core.DTOs;

public class ComplaintDto
{
    public int Id { get; set; }
    public string ComplaintNumber { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ComplaintStatus Status { get; set; }
    public string StatusName { get; set; } = string.Empty;
    public PriorityLevel Priority { get; set; }
    public string PriorityName { get; set; } = string.Empty;
    public bool IsAiCategorized { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string LocationDescription { get; set; } = string.Empty;
    public DateTime? SlaDeadline { get; set; }
    public bool IsSlaBreached { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public int CitizenId { get; set; }
    public string CitizenName { get; set; } = string.Empty;
    public int CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string CategoryIcon { get; set; } = string.Empty;
    public string CategoryColor { get; set; } = string.Empty;
    public int? AssignedAdminId { get; set; }
    public string AssignedAdminName { get; set; } = string.Empty;
    public List<AttachmentDto> Attachments { get; set; } = new();
    public bool HasFeedback { get; set; }
    public List<StatusHistoryDto> StatusHistories { get; set; } = new();
}

public class CreateComplaintDto
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int? CategoryId { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public string LocationDescription { get; set; } = string.Empty;
}

public class ComplaintFilterDto
{
    public string? SearchTerm { get; set; }
    public string? Status { get; set; }
    public int? CategoryId { get; set; }
    public string? Priority { get; set; }
    public DateTime? DateFrom { get; set; }
    public DateTime? DateTo { get; set; }
    public bool? IsSlaBreached { get; set; }
    public int? AssignedAdminId { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 10;
    public string SortBy { get; set; } = "CreatedAt";
    public bool SortDescending { get; set; } = true;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}

public class AttachmentDto
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty;
    public long FileSizeBytes { get; set; }
}

public class StatusHistoryDto
{
    public int Id { get; set; }
    public string? FromStatus { get; set; }
    public string ToStatus { get; set; } = string.Empty;
    public string Note { get; set; } = string.Empty;
    public string ChangedByName { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}
