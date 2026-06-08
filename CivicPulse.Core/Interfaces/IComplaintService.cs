using CivicPulse.Core.DTOs;
using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Interfaces;

public interface IComplaintService
{
    Task<ComplaintDto> CreateComplaintAsync(CreateComplaintDto dto, int citizenId);
    Task<ComplaintDto> GetByIdAsync(int id);
    Task<PagedResult<ComplaintDto>> GetAllAsync(ComplaintFilterDto filter);
    Task<PagedResult<ComplaintDto>> GetByCitizenAsync(int citizenId, ComplaintFilterDto filter);
    Task<ComplaintDto> UpdateStatusAsync(int id, ComplaintStatus newStatus, string note, int adminId);
    Task<ComplaintDto> ReopenComplaintAsync(int id, string reason, int citizenId);
    Task<bool> ArchiveComplaintAsync(int id);
    Task<string> GenerateComplaintNumberAsync();
    Task<List<ComplaintDto>> GetSlaBreachedAsync();
    Task<List<ComplaintDto>> GetSlaApproachingAsync(int thresholdPercent = 80);
    Task CheckAndUpdateSlaStatusAsync();
    Task<AdminDashboardStatsDto> GetAdminDashboardStatsAsync();
    Task<CitizenDashboardDto> GetCitizenDashboardStatsAsync(int citizenId);
    Task<List<ComplaintDto>> GetRecentComplaintsAsync(int count = 5);
    Task<byte[]> ExportComplaintsToCsvAsync(ComplaintFilterDto filter);
}
