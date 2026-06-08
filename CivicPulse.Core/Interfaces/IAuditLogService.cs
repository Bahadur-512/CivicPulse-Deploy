using CivicPulse.Core.DTOs;

namespace CivicPulse.Core.Interfaces;

public interface IAuditLogService
{
    Task LogAsync(string action, string entityType, int? entityId, object? oldValues, object? newValues, int? userId, string description, string? ipAddress = null);
    Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter);
    Task<List<AuditLogDto>> GetForComplaintAsync(int complaintId);
    Task<List<AuditLogDto>> GetForUserAsync(int userId);
}
