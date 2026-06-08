using CivicPulse.Core.DTOs;

namespace CivicPulse.Core.Interfaces;

public interface IAnalyticsService
{
    Task<AdminDashboardStatsDto> GetAdminStatsAsync();
    Task<List<CategoryBreakdownDto>> GetComplaintsByCategoryAsync(DateTime? from = null, DateTime? to = null);
    Task<List<AreaHeatmapDto>> GetComplaintsByAreaAsync();
    Task<List<MonthlyTrendDto>> GetMonthlyTrendAsync(int months = 12);
    Task<List<ResolutionTimeDto>> GetAverageResolutionTimeAsync();
    Task<List<PriorityDistributionDto>> GetPriorityDistributionAsync();
    Task<SlaComplianceDto> GetSlaComplianceRateAsync();
}
