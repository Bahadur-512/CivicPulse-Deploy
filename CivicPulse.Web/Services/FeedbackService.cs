using Microsoft.EntityFrameworkCore;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class FeedbackService : IFeedbackService
{
    private readonly IRepository<Feedback> _feedbackRepo;
    private readonly IRepository<Complaint> _complaintRepo;
    private readonly IAuditLogService _auditLog;

    public FeedbackService(IRepository<Feedback> feedbackRepo, IRepository<Complaint> complaintRepo,
        IAuditLogService auditLog)
    {
        _feedbackRepo = feedbackRepo;
        _complaintRepo = complaintRepo;
        _auditLog = auditLog;
    }

    public async Task<FeedbackDto> CreateAsync(CreateFeedbackDto dto)
    {
        if (dto.Rating < 1 || dto.Rating > 5)
            throw new ArgumentException("Rating must be between 1 and 5.");

        var existing = await _feedbackRepo.Query()
            .FirstOrDefaultAsync(f => f.ComplaintId == dto.ComplaintId);
        if (existing != null)
            throw new InvalidOperationException("Feedback has already been submitted for this complaint.");

        var complaint = await _complaintRepo.Query()
            .Include(c => c.Feedback)
            .FirstOrDefaultAsync(c => c.Id == dto.ComplaintId)
            ?? throw new InvalidOperationException("Complaint not found.");

        if (complaint.Status != ComplaintStatus.Resolved)
            throw new InvalidOperationException("Feedback can only be submitted for resolved complaints.");

        if (complaint.Feedback != null)
            throw new InvalidOperationException("Feedback has already been submitted for this complaint.");

        var feedback = new Feedback
        {
            ComplaintId = dto.ComplaintId,
            CitizenId = dto.CitizenId,
            Rating = dto.Rating,
            Comment = (dto.Comment ?? "").Trim(),
            WasResolutionSatisfactory = dto.WasResolutionSatisfactory,
            WasResponseTimely = dto.WasResponseTimely
        };

        await _feedbackRepo.AddAsync(feedback);
        await _feedbackRepo.SaveChangesAsync();

        await _auditLog.LogAsync("FEEDBACK_SUBMITTED", "Feedback", feedback.Id,
            null, new { Rating = dto.Rating, ComplaintId = dto.ComplaintId },
            dto.CitizenId, $"Citizen submitted {dto.Rating}-star feedback for complaint {complaint.ComplaintNumber}");

        return await MapToDtoAsync(feedback);
    }

    public async Task<FeedbackDto?> GetByComplaintAsync(int complaintId)
    {
        var feedback = await _feedbackRepo.Query()
            .Include(f => f.Citizen)
            .Include(f => f.Complaint)
            .FirstOrDefaultAsync(f => f.ComplaintId == complaintId);

        return feedback == null ? null : await MapToDtoAsync(feedback);
    }

    public async Task<bool> HasFeedbackAsync(int complaintId, int citizenId)
    {
        return await _feedbackRepo.Query()
            .AnyAsync(f => f.ComplaintId == complaintId && f.CitizenId == citizenId);
    }

    public async Task<List<FeedbackDto>> GetAllAsync()
    {
        var feedbacks = await _feedbackRepo.Query()
            .Include(f => f.Citizen)
            .Include(f => f.Complaint)
            .Where(f => f.Complaint != null && (f.Complaint.Status == ComplaintStatus.Resolved || f.Complaint.Status == ComplaintStatus.Closed))
            .OrderByDescending(f => f.CreatedAt)
            .ToListAsync();

        var result = new List<FeedbackDto>();
        foreach (var f in feedbacks)
            result.Add(await MapToDtoAsync(f));

        return result;
    }

    public async Task<PagedResult<FeedbackDto>> GetAllPagedAsync(int page, int pageSize, string? search = null)
    {
        IQueryable<Feedback> query = _feedbackRepo.Query()
            .Include(f => f.Citizen)
            .Include(f => f.Complaint)
            .Where(f => f.Complaint != null && (f.Complaint.Status == ComplaintStatus.Resolved || f.Complaint.Status == ComplaintStatus.Closed));

        if (!string.IsNullOrWhiteSpace(search))
        {
            var term = search.Trim().ToLower();
            query = query.Where(f =>
                (f.Citizen != null && f.Citizen.FullName.ToLower().Contains(term)) ||
                (f.Complaint != null && f.Complaint.ComplaintNumber.ToLower().Contains(term)));
        }

        var totalCount = await query.CountAsync();

        var feedbacks = await query
            .OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        var items = new List<FeedbackDto>();
        foreach (var f in feedbacks)
            items.Add(await MapToDtoAsync(f));

        return new PagedResult<FeedbackDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<double> GetAverageRatingAsync()
    {
        var all = await _feedbackRepo.Query()
            .Include(f => f.Complaint)
            .Where(f => f.Complaint != null && (f.Complaint.Status == ComplaintStatus.Resolved || f.Complaint.Status == ComplaintStatus.Closed))
            .ToListAsync();
        if (all.Count == 0) return 0;
        return all.Average(f => (double)f.Rating);
    }

    public async Task<int> GetCountAsync()
    {
        return await _feedbackRepo.Query()
            .Include(f => f.Complaint)
            .Where(f => f.Complaint != null && (f.Complaint.Status == ComplaintStatus.Resolved || f.Complaint.Status == ComplaintStatus.Closed))
            .CountAsync();
    }

    private Task<FeedbackDto> MapToDtoAsync(Feedback f)
    {
        return Task.FromResult(new FeedbackDto
        {
            Id = f.Id,
            ComplaintId = f.ComplaintId,
            ComplaintNumber = f.Complaint?.ComplaintNumber ?? "",
            CitizenId = f.CitizenId,
            CitizenName = f.Citizen?.FullName ?? "Unknown",
            Rating = f.Rating,
            Comment = f.Comment,
            WasResolutionSatisfactory = f.WasResolutionSatisfactory,
            WasResponseTimely = f.WasResponseTimely,
            CreatedAt = f.CreatedAt
        });
    }
}
