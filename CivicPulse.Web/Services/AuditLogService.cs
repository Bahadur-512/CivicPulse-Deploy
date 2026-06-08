using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class AuditLogService : IAuditLogService
{
    private readonly IRepository<AuditLog> _auditLogRepo;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public AuditLogService(IRepository<AuditLog> auditLogRepo, IHttpContextAccessor httpContextAccessor)
    {
        _auditLogRepo = auditLogRepo;
        _httpContextAccessor = httpContextAccessor;
    }

    public async Task LogAsync(string action, string entityType, int? entityId, object? oldValues,
        object? newValues, int? userId, string description, string? ipAddress = null)
    {
        ipAddress ??= _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString() ?? "unknown";

        var auditLog = new AuditLog
        {
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            OldValues = oldValues != null ? JsonSerializer.Serialize(oldValues) : "",
            NewValues = newValues != null ? JsonSerializer.Serialize(newValues) : "",
            IpAddress = ipAddress,
            UserId = userId,
            Description = description
        };

        await _auditLogRepo.AddAsync(auditLog);
        await _auditLogRepo.SaveChangesAsync();
    }

    public async Task<PagedResult<AuditLogDto>> GetLogsAsync(AuditLogFilterDto filter)
    {
        var query = _auditLogRepo.Query()
            .Include(a => a.User)
            .AsQueryable();

        if (!string.IsNullOrEmpty(filter.Action))
            query = query.Where(a => a.Action == filter.Action);

        if (!string.IsNullOrEmpty(filter.EntityType))
            query = query.Where(a => a.EntityType == filter.EntityType);

        if (filter.UserId.HasValue)
            query = query.Where(a => a.UserId == filter.UserId);

        if (filter.DateFrom.HasValue)
            query = query.Where(a => a.CreatedAt >= filter.DateFrom.Value);

        if (filter.DateTo.HasValue)
            query = query.Where(a => a.CreatedAt <= filter.DateTo.Value);

        var total = await query.CountAsync();
        var logs = await query.OrderByDescending(a => a.CreatedAt)
            .Skip((filter.Page - 1) * filter.PageSize)
            .Take(filter.PageSize)
            .ToListAsync();

        return new PagedResult<AuditLogDto>
        {
            Items = logs.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = filter.Page,
            PageSize = filter.PageSize
        };
    }

    public async Task<List<AuditLogDto>> GetForComplaintAsync(int complaintId)
    {
        var logs = await _auditLogRepo.Query()
            .Include(a => a.User)
            .Where(a => a.EntityId == complaintId && a.EntityType == "Complaint")
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    public async Task<List<AuditLogDto>> GetForUserAsync(int userId)
    {
        var logs = await _auditLogRepo.Query()
            .Include(a => a.User)
            .Where(a => a.UserId == userId)
            .OrderByDescending(a => a.CreatedAt)
            .Take(50)
            .ToListAsync();

        return logs.Select(MapToDto).ToList();
    }

    private static AuditLogDto MapToDto(AuditLog log) => new()
    {
        Id = log.Id,
        Action = log.Action,
        EntityType = log.EntityType,
        EntityId = log.EntityId,
        OldValues = log.OldValues,
        NewValues = log.NewValues,
        IpAddress = log.IpAddress,
        UserId = log.UserId,
        UserName = log.User?.FullName ?? "System",
        Description = log.Description,
        CreatedAt = log.CreatedAt
    };
}
