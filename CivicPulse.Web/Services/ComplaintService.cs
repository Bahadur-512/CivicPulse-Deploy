using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class ComplaintService : IComplaintService
{
    private readonly IRepository<Complaint> _complaintRepo;
    private readonly IRepository<Category> _categoryRepo;
    private readonly IRepository<StatusHistory> _statusHistoryRepo;
    private readonly IRepository<ApplicationUser> _userRepo;
    private readonly CategorizationEngine _categorizationEngine;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IConfiguration _configuration;
    private readonly IEmailNotificationService _email;

    public ComplaintService(IRepository<Complaint> complaintRepo, IRepository<Category> categoryRepo,
        IRepository<StatusHistory> statusHistoryRepo, IRepository<ApplicationUser> userRepo,
        CategorizationEngine categorizationEngine, INotificationService notificationService,
        IAuditLogService auditLogService, IConfiguration configuration,
        IEmailNotificationService email)
    {
        _complaintRepo = complaintRepo;
        _categoryRepo = categoryRepo;
        _statusHistoryRepo = statusHistoryRepo;
        _userRepo = userRepo;
        _categorizationEngine = categorizationEngine;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _configuration = configuration;
        _email = email;
    }

    public async Task<ComplaintDto> CreateComplaintAsync(CreateComplaintDto dto, int citizenId)
    {
        var categoryId = dto.CategoryId ?? 0;

        if (categoryId == 0)
        {
            var result = await _categorizationEngine.CategorizeAsync(dto.Title, dto.Description);
            if (result.IsConfident)
                categoryId = result.CategoryId;
        }

        if (categoryId == 0)
        {
            var defaultCategory = await _categoryRepo.Query().FirstOrDefaultAsync(c => c.IsActive);
            if (defaultCategory != null)
                categoryId = defaultCategory.Id;
        }

        var category = await _categoryRepo.Query()
            .FirstOrDefaultAsync(c => c.Id == categoryId)
            ?? throw new InvalidOperationException("Invalid category.");

        var priority = category.DefaultPriority;

        var complaint = new Complaint
        {
            ComplaintNumber = await GenerateComplaintNumberAsync(),
            Title = dto.Title,
            Description = dto.Description,
            Status = ComplaintStatus.Pending,
            Priority = priority,
            IsAiCategorized = dto.CategoryId == null || dto.CategoryId == 0,
            Latitude = dto.Latitude,
            Longitude = dto.Longitude,
            LocationDescription = dto.LocationDescription ?? "",
            CitizenId = citizenId,
            CategoryId = category.Id,
            SlaDeadline = DateTime.UtcNow.AddDays(category.DefaultSlaDays)
        };

        await _complaintRepo.AddAsync(complaint);
        await _complaintRepo.SaveChangesAsync();

        var statusHistory = new StatusHistory
        {
            ComplaintId = complaint.Id,
            FromStatus = null,
            ToStatus = ComplaintStatus.Pending,
            Note = "Complaint submitted",
            ChangedById = citizenId
        };
        await _statusHistoryRepo.AddAsync(statusHistory);

        await _notificationService.CreateAsync(citizenId,
            "Complaint Submitted",
            $"Your complaint {complaint.ComplaintNumber} has been submitted successfully.",
            NotificationType.StatusUpdate, complaint.Id);

        await _auditLogService.LogAsync("COMPLAINT_CREATED", "Complaint", complaint.Id,
            null, JsonSerializer.Serialize(new { complaint.ComplaintNumber, complaint.Title }),
            citizenId, $"Complaint {complaint.ComplaintNumber} created");

        await _statusHistoryRepo.SaveChangesAsync();

        var citizen = await _userRepo.Query().FirstOrDefaultAsync(u => u.Id == citizenId);
        if (citizen?.Email != null)
        {
            await _email.SendComplaintSubmittedAsync(
                toEmail: citizen.Email,
                citizenName: citizen.FullName,
                complaintNumber: complaint.ComplaintNumber,
                complaintTitle: complaint.Title,
                category: category.Name ?? ""
            );
        }

        return await GetByIdAsync(complaint.Id);
    }

    public async Task<ComplaintDto> GetByIdAsync(int id)
    {
        var complaint = await _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Include(c => c.AssignedAdmin)
            .Include(c => c.Attachments)
            .Include(c => c.StatusHistories).ThenInclude(s => s.ChangedBy)
            .Include(c => c.Feedback)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Complaint not found.");

        return MapToDto(complaint);
    }

    public async Task<PagedResult<ComplaintDto>> GetAllAsync(ComplaintFilterDto filter)
    {
        var query = _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Include(c => c.AssignedAdmin)
            .Where(c => !c.IsArchived)
            .AsQueryable();

        query = ApplyFilter(query, filter);

        var total = await query.CountAsync();

        query = filter.SortDescending
            ? query.OrderByDescending(c => EF.Property<object>(c, filter.SortBy))
            : query.OrderBy(c => EF.Property<object>(c, filter.SortBy));

        var complaints = await query
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ComplaintDto>
        {
            Items = complaints.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<PagedResult<ComplaintDto>> GetByCitizenAsync(int citizenId, ComplaintFilterDto filter)
    {
        var query = _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Include(c => c.Feedback)
            .Where(c => c.CitizenId == citizenId && !c.IsArchived)
            .AsQueryable();

        query = ApplyFilter(query, filter);

        var total = await query.CountAsync();
        var complaints = await query
            .OrderByDescending(c => c.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<ComplaintDto>
        {
            Items = complaints.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    private IQueryable<Complaint> ApplyFilter(IQueryable<Complaint> query, ComplaintFilterDto filter)
    {
        if (!string.IsNullOrEmpty(filter.SearchTerm))
        {
            var search = filter.SearchTerm.ToLower();
            query = query.Where(c => c.ComplaintNumber.ToLower().Contains(search)
                || c.Title.ToLower().Contains(search)
                || c.Description.ToLower().Contains(search));
        }

        if (!string.IsNullOrWhiteSpace(filter.Status))
            if (Enum.TryParse<ComplaintStatus>(filter.Status, true, out var status))
                query = query.Where(c => c.Status == status);

        if (filter.CategoryId.HasValue)
            query = query.Where(c => c.CategoryId == filter.CategoryId.Value);

        if (!string.IsNullOrWhiteSpace(filter.Priority))
            if (Enum.TryParse<PriorityLevel>(filter.Priority, true, out var priority))
                query = query.Where(c => c.Priority == priority);

        if (filter.DateFrom.HasValue)
            query = query.Where(c => c.CreatedAt >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(c => c.CreatedAt <= filter.DateTo.Value);

        if (filter.IsSlaBreached.HasValue)
            query = query.Where(c => c.IsSlaBreached == filter.IsSlaBreached.Value);

        if (filter.AssignedAdminId.HasValue)
            query = query.Where(c => c.AssignedAdminId == filter.AssignedAdminId.Value);

        return query;
    }

    public async Task<ComplaintDto> UpdateStatusAsync(int id, ComplaintStatus newStatus, string note, int adminId)
    {
        var complaint = await _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .FirstOrDefaultAsync(c => c.Id == id)
            ?? throw new KeyNotFoundException("Complaint not found.");

        var oldStatus = complaint.Status;
        if (!IsValidTransition(oldStatus, newStatus))
            throw new InvalidOperationException($"Cannot transition from {oldStatus} to {newStatus}.");

        complaint.Status = newStatus;
        complaint.UpdatedAt = DateTime.UtcNow;

        if (newStatus == ComplaintStatus.Resolved)
        {
            complaint.ResolvedAt = DateTime.UtcNow;
            complaint.FeedbackRequested = true;
        }

        _complaintRepo.Update(complaint);

        var statusHistory = new StatusHistory
        {
            ComplaintId = complaint.Id,
            FromStatus = oldStatus,
            ToStatus = newStatus,
            Note = note,
            ChangedById = adminId
        };
        await _statusHistoryRepo.AddAsync(statusHistory);

        await _notificationService.CreateAsync(complaint.CitizenId,
            "Status Updated",
            $"Your complaint {complaint.ComplaintNumber} status changed to {newStatus}.",
            NotificationType.StatusUpdate, complaint.Id);

        if (newStatus == ComplaintStatus.Resolved)
        {
            await _notificationService.CreateAsync(complaint.CitizenId,
                "Feedback Requested",
                $"Your complaint {complaint.ComplaintNumber} has been resolved. Please provide feedback.",
                NotificationType.FeedbackRequest, complaint.Id);
        }

        await _auditLogService.LogAsync("STATUS_CHANGED", "Complaint", complaint.Id,
            JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
            JsonSerializer.Serialize(new { Status = newStatus.ToString() }),
            adminId, $"Status changed from {oldStatus} to {newStatus} for {complaint.ComplaintNumber}");

        if (complaint.Citizen?.Email != null)
        {
            await _email.SendStatusUpdateAsync(
                toEmail: complaint.Citizen.Email,
                citizenName: complaint.Citizen.FullName,
                complaintNumber: complaint.ComplaintNumber,
                complaintTitle: complaint.Title,
                oldStatus: oldStatus.ToString(),
                newStatus: newStatus.ToString(),
                note: note
            );

            if (newStatus == ComplaintStatus.Resolved)
            {
                await _email.SendFeedbackRequestAsync(
                    toEmail: complaint.Citizen.Email,
                    citizenName: complaint.Citizen.FullName,
                    complaintNumber: complaint.ComplaintNumber,
                    complaintId: complaint.Id
                );
            }
        }

        await _complaintRepo.SaveChangesAsync();

        return await GetByIdAsync(complaint.Id);
    }

    private static bool IsValidTransition(ComplaintStatus from, ComplaintStatus to)
    {
        return (from, to) switch
        {
            (ComplaintStatus.Pending, ComplaintStatus.UnderReview) => true,
            (ComplaintStatus.Pending, ComplaintStatus.Rejected) => true,
            (ComplaintStatus.UnderReview, ComplaintStatus.InProgress) => true,
            (ComplaintStatus.UnderReview, ComplaintStatus.Rejected) => true,
            (ComplaintStatus.InProgress, ComplaintStatus.Resolved) => true,
            (ComplaintStatus.InProgress, ComplaintStatus.Rejected) => true,
            (ComplaintStatus.Resolved, ComplaintStatus.Closed) => true,
            (ComplaintStatus.Resolved, ComplaintStatus.Reopened) => true,
            (ComplaintStatus.Closed, ComplaintStatus.Reopened) => true,
            (ComplaintStatus.Reopened, ComplaintStatus.InProgress) => true,
            (ComplaintStatus.Reopened, ComplaintStatus.Rejected) => true,
            _ => false
        };
    }

    public async Task<ComplaintDto> ReopenComplaintAsync(int id, string reason, int citizenId)
    {
        var complaint = await _complaintRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("Complaint not found.");

        if (complaint.CitizenId != citizenId)
            throw new UnauthorizedAccessException("You can only reopen your own complaints.");

        if (complaint.Status != ComplaintStatus.Resolved && complaint.Status != ComplaintStatus.Closed)
            throw new InvalidOperationException("Only resolved or closed complaints can be reopened.");

        if (complaint.ResolvedAt.HasValue && (DateTime.UtcNow - complaint.ResolvedAt.Value).TotalDays > 7)
            throw new InvalidOperationException("Complaints can only be reopened within 7 days of resolution.");

        if (complaint.ReopenCount >= 3)
            throw new InvalidOperationException("Maximum reopen limit reached.");

        var oldStatus = complaint.Status;
        complaint.Status = ComplaintStatus.Reopened;
        complaint.ReopenedAt = DateTime.UtcNow;
        complaint.ReopenReason = reason;
        complaint.ReopenCount++;
        complaint.SlaDeadline = DateTime.UtcNow.AddDays(2);
        complaint.IsSlaBreached = false;
        _complaintRepo.Update(complaint);

        var statusHistory = new StatusHistory
        {
            ComplaintId = complaint.Id,
            FromStatus = oldStatus,
            ToStatus = ComplaintStatus.Reopened,
            Note = $"Reopened: {reason}",
            ChangedById = citizenId
        };
        await _statusHistoryRepo.AddAsync(statusHistory);

        await _auditLogService.LogAsync("COMPLAINT_REOPENED", "Complaint", complaint.Id,
            JsonSerializer.Serialize(new { Status = oldStatus.ToString() }),
            JsonSerializer.Serialize(new { Status = "Reopened", Reason = reason }),
            citizenId, $"Complaint {complaint.ComplaintNumber} reopened: {reason}");

        await _complaintRepo.SaveChangesAsync();
        return await GetByIdAsync(complaint.Id);
    }

    public async Task<bool> ArchiveComplaintAsync(int id)
    {
        var complaint = await _complaintRepo.GetByIdAsync(id);
        if (complaint == null) return false;

        complaint.IsArchived = true;
        _complaintRepo.Update(complaint);
        await _complaintRepo.SaveChangesAsync();
        return true;
    }

    public async Task<string> GenerateComplaintNumberAsync()
    {
        var year = DateTime.UtcNow.Year;
        var prefix = $"CP-{year}-";
        var lastComplaint = await _complaintRepo.Query()
            .Where(c => c.ComplaintNumber.StartsWith(prefix))
            .OrderByDescending(c => c.ComplaintNumber)
            .FirstOrDefaultAsync();

        int sequence = 1;
        if (lastComplaint != null)
        {
            var lastNumber = lastComplaint.ComplaintNumber[(prefix.Length)..];
            sequence = int.Parse(lastNumber) + 1;
        }

        return $"{prefix}{sequence:D5}";
    }

    public async Task<List<ComplaintDto>> GetSlaBreachedAsync()
    {
        var complaints = await _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Where(c => c.IsSlaBreached && !c.IsArchived
                        && c.Status != ComplaintStatus.Resolved
                        && c.Status != ComplaintStatus.Closed)
            .OrderByDescending(c => c.Priority)
            .ToListAsync();

        return complaints.Select(MapToDto).ToList();
    }

    public async Task<List<ComplaintDto>> GetSlaApproachingAsync(int thresholdPercent = 80)
    {
        var complaints = await _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Where(c => !c.IsArchived && c.SlaDeadline.HasValue
                        && c.Status != ComplaintStatus.Resolved
                        && c.Status != ComplaintStatus.Closed)
            .ToListAsync();

        var result = complaints.Where(c =>
        {
            if (!c.SlaDeadline.HasValue) return false;
            var totalDuration = c.SlaDeadline.Value - c.CreatedAt;
            var elapsed = DateTime.UtcNow - c.CreatedAt;
            var percent = totalDuration.TotalSeconds > 0
                ? (elapsed.TotalSeconds / totalDuration.TotalSeconds) * 100
                : 100;
            return percent >= thresholdPercent && !c.IsSlaBreached;
        })
        .OrderByDescending(c => c.Priority)
        .Select(MapToDto)
        .ToList();

        return result;
    }

    public async Task CheckAndUpdateSlaStatusAsync()
    {
        var complaints = await _complaintRepo.Query()
            .Where(c => !c.IsArchived && c.SlaDeadline.HasValue
                        && c.Status != ComplaintStatus.Resolved
                        && c.Status != ComplaintStatus.Closed)
            .ToListAsync();

        foreach (var complaint in complaints)
        {
            if (complaint.SlaDeadline.HasValue && DateTime.UtcNow > complaint.SlaDeadline.Value)
            {
                complaint.IsSlaBreached = true;
                _complaintRepo.Update(complaint);
            }
        }

        await _complaintRepo.SaveChangesAsync();
    }

    public async Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync()
    {
        var allComplaints = await _complaintRepo.Query()
            .Where(c => !c.IsArchived)
            .ToListAsync();

        var resolved = allComplaints.Where(c => c.Status == ComplaintStatus.Resolved && c.ResolvedAt.HasValue).ToList();

        return new AdminDashboardStatsDto
        {
            TotalComplaints = allComplaints.Count,
            PendingCount = allComplaints.Count(c => c.Status == ComplaintStatus.Pending),
            InProgressCount = allComplaints.Count(c => c.Status == ComplaintStatus.InProgress),
            ResolvedCount = resolved.Count,
            SlaBreachedCount = allComplaints.Count(c => c.IsSlaBreached),
            AverageResolutionDays = resolved.Any()
                ? Math.Round(resolved.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalDays), 1) : 0
        };
    }

    public async Task<CitizenDashboardDto> GetCitizenDashboardStatsAsync(int citizenId)
    {
        var complaints = await _complaintRepo.Query()
            .Where(c => c.CitizenId == citizenId && !c.IsArchived)
            .OrderByDescending(c => c.CreatedAt)
            .ToListAsync();

        return new CitizenDashboardDto
        {
            TotalComplaints = complaints.Count,
            PendingCount = complaints.Count(c => c.Status == ComplaintStatus.Pending),
            InProgressCount = complaints.Count(c => c.Status == ComplaintStatus.InProgress),
            ResolvedCount = complaints.Count(c => c.Status == ComplaintStatus.Resolved),
            RecentComplaints = complaints.Take(5).Select(MapToDto).ToList()
        };
    }

    public async Task<List<ComplaintDto>> GetRecentComplaintsAsync(int count = 5)
    {
        var complaints = await _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Where(c => !c.IsArchived)
            .OrderByDescending(c => c.CreatedAt)
            .Take(count)
            .ToListAsync();

        return complaints.Select(MapToDto).ToList();
    }

    public async Task<byte[]> ExportComplaintsToCsvAsync(ComplaintFilterDto filter)
    {
        var complaints = await GetAllAsync(filter);
        var csv = new System.Text.StringBuilder();

        csv.AppendLine("ComplaintNumber,Title,Category,Status,Priority,Citizen,CreatedDate,SLA%");

        foreach (var c in complaints.Items)
        {
            csv.AppendLine($"\"{c.ComplaintNumber}\",\"{c.Title}\",\"{c.CategoryName}\",{c.StatusName},{c.PriorityName},\"{c.CitizenName}\",{c.CreatedAt:yyyy-MM-dd},{c.IsSlaBreached}");
        }

        return System.Text.Encoding.UTF8.GetBytes(csv.ToString());
    }

    private ComplaintDto MapToDto(Complaint c)
    {
        var slaPercent = 0.0;
        if (c.SlaDeadline.HasValue)
        {
            var total = (c.SlaDeadline.Value - c.CreatedAt).TotalSeconds;
            var elapsed = (DateTime.UtcNow - c.CreatedAt).TotalSeconds;
            slaPercent = total > 0 ? Math.Round(Math.Min(elapsed / total * 100, 100), 1) : 100;
        }

        return new ComplaintDto
        {
            Id = c.Id,
            ComplaintNumber = c.ComplaintNumber,
            Title = c.Title,
            Description = c.Description,
            Status = c.Status,
            StatusName = c.Status.ToString(),
            Priority = c.Priority,
            PriorityName = c.Priority.ToString(),
            IsAiCategorized = c.IsAiCategorized,
            Latitude = c.Latitude,
            Longitude = c.Longitude,
            LocationDescription = c.LocationDescription,
            SlaDeadline = c.SlaDeadline,
            IsSlaBreached = c.IsSlaBreached,
            ResolvedAt = c.ResolvedAt,
            CreatedAt = c.CreatedAt,
            UpdatedAt = c.UpdatedAt,
            CitizenId = c.CitizenId,
            CitizenName = c.Citizen?.FullName ?? "",
            CategoryId = c.CategoryId,
            CategoryName = c.Category?.Name ?? "",
            CategoryIcon = c.Category?.IconName ?? "",
            CategoryColor = c.Category?.ColorHex ?? "",
            AssignedAdminId = c.AssignedAdminId,
            AssignedAdminName = c.AssignedAdmin?.FullName ?? "",
            HasFeedback = c.Feedback != null,
            Attachments = c.Attachments?.Select(a => new AttachmentDto
            {
                Id = a.Id,
                FileName = a.FileName,
                FilePath = a.FilePath,
                FileType = a.FileType,
                FileSizeBytes = a.FileSizeBytes
            }).ToList() ?? new(),
            StatusHistories = c.StatusHistories?.OrderByDescending(s => s.CreatedAt).Select(s => new StatusHistoryDto
            {
                Id = s.Id,
                FromStatus = s.FromStatus?.ToString(),
                ToStatus = s.ToStatus.ToString(),
                Note = s.Note,
                ChangedByName = s.ChangedBy?.FullName ?? "",
                CreatedAt = s.CreatedAt
            }).ToList() ?? new()
        };
    }
}
