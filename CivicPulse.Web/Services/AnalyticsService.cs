using Microsoft.EntityFrameworkCore;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly IRepository<Complaint> _complaintRepo;
    private readonly IRepository<Feedback> _feedbackRepo;

    public AnalyticsService(IRepository<Complaint> complaintRepo, IRepository<Feedback> feedbackRepo)
    {
        _complaintRepo = complaintRepo;
        _feedbackRepo = feedbackRepo;
    }

    public async Task<AdminDashboardStatsDto> GetAdminStatsAsync()
    {
        var now = DateTime.UtcNow;
        var allComplaints = await _complaintRepo.Query()
            .Where(c => !c.IsArchived)
            .ToListAsync();

        var resolvedComplaints = allComplaints.Where(c =>
            c.Status == ComplaintStatus.Resolved && c.ResolvedAt.HasValue).ToList();

        var avgResolutionDays = resolvedComplaints.Any()
            ? Math.Abs(resolvedComplaints.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalDays))
            : 0;

        var totalTracked = allComplaints.Count(c => c.SlaDeadline.HasValue);
        var breached = allComplaints.Count(c => c.IsSlaBreached);

        return new AdminDashboardStatsDto
        {
            TotalComplaints = allComplaints.Count,
            PendingCount = allComplaints.Count(c => c.Status == ComplaintStatus.Pending),
            InProgressCount = allComplaints.Count(c => c.Status == ComplaintStatus.InProgress),
            ResolvedCount = resolvedComplaints.Count,
            SlaBreachedCount = breached,
            AverageResolutionDays = Math.Round(avgResolutionDays, 1),
            SlaCompliancePercent = totalTracked > 0 ? Math.Round((double)(totalTracked - breached) / totalTracked * 100, 1) : 100
        };
    }

    public async Task<List<CategoryBreakdownDto>> GetComplaintsByCategoryAsync(DateTime? from = null, DateTime? to = null)
    {
        var query = _complaintRepo.Query()
            .Include(c => c.Category)
            .Where(c => !c.IsArchived);

        if (from.HasValue) query = query.Where(c => c.CreatedAt >= from.Value);
        if (to.HasValue) query = query.Where(c => c.CreatedAt <= to.Value);

        var grouped = await query
            .GroupBy(c => new { c.CategoryId, c.Category.Name, c.Category.ColorHex })
            .Select(g => new CategoryBreakdownDto
            {
                CategoryName = g.Key.Name,
                Count = g.Count(),
                ColorHex = g.Key.ColorHex
            })
            .OrderByDescending(c => c.Count)
            .ToListAsync();

        return grouped;
    }

    public async Task<List<AreaHeatmapDto>> GetComplaintsByAreaAsync()
    {
        return await _complaintRepo.Query()
            .Where(c => !c.IsArchived && c.Latitude.HasValue && c.Longitude.HasValue)
            .Select(c => new AreaHeatmapDto
            {
                Latitude = c.Latitude!.Value,
                Longitude = c.Longitude!.Value,
                Priority = c.Priority.ToString(),
                ComplaintNumber = c.ComplaintNumber,
                CategoryName = c.Category.Name,
                Status = c.Status.ToString()
            })
            .ToListAsync();
    }

    public async Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months = 12)
    {
        var now = DateTime.UtcNow;
        var startDate = now.AddMonths(-months);

        var complaints = await _complaintRepo.Query()
            .Where(c => c.CreatedAt >= startDate && !c.IsArchived)
            .ToListAsync();

        var grouped = complaints
            .GroupBy(c => new { c.CreatedAt.Year, c.CreatedAt.Month })
            .ToDictionary(g => (g.Key.Year, g.Key.Month), g => g.Count());

        var result = new List<MonthlyTrendDto>();
        for (int i = months; i >= 0; i--)
        {
            var date = now.AddMonths(-i);
            var key = (date.Year, date.Month);
            result.Add(new MonthlyTrendDto
            {
                Year = date.Year,
                Month = date.ToString("MMM"),
                Count = grouped.TryGetValue(key, out var count) ? count : 0
            });
        }

        return result;
    }

    public async Task<List<ResolutionTimeDto>> GetAverageResolutionTimeAsync()
    {
        var twelveMonthsAgo = DateTime.UtcNow.AddMonths(-12);
        var resolved = await _complaintRepo.Query()
            .Where(c => c.Status == ComplaintStatus.Resolved && c.ResolvedAt.HasValue
                        && c.ResolvedAt >= twelveMonthsAgo)
            .ToListAsync();

        return resolved
            .GroupBy(c => new { c.ResolvedAt!.Value.Year, c.ResolvedAt.Value.Month })
            .Select(g => new ResolutionTimeDto
            {
                Month = new DateTime(g.Key.Year, g.Key.Month, 1).ToString("MMM yyyy"),
                AvgDays = Math.Round(g.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalDays), 1)
            })
            .OrderBy(r => r.Month)
            .ToList();
    }

    public async Task<List<PriorityDistributionDto>> GetPriorityDistributionAsync()
    {
        var colorMap = new Dictionary<string, string>
        {
            ["Critical"] = "#DC2626", ["High"] = "#EA580C",
            ["Medium"] = "#D97706", ["Low"] = "#65A30D"
        };

        return await _complaintRepo.Query()
            .Where(c => !c.IsArchived)
            .GroupBy(c => c.Priority)
            .Select(g => new PriorityDistributionDto
            {
                Priority = g.Key.ToString(),
                Count = g.Count(),
                Color = ""
            })
            .ToListAsync();

    }

    public async Task<SlaComplianceDto> GetSlaComplianceRateAsync()
    {
        var tracked = await _complaintRepo.Query()
            .Where(c => !c.IsArchived && c.SlaDeadline.HasValue)
            .ToListAsync();

        var total = tracked.Count;
        var breached = tracked.Count(c => c.IsSlaBreached);

        return new SlaComplianceDto
        {
            ComplianceRate = total > 0 ? Math.Round((double)(total - breached) / total * 100, 1) : 100,
            BreachedCount = breached,
            TotalTracked = total
        };
    }
}
