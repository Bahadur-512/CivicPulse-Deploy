using Microsoft.EntityFrameworkCore;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class SlaService : ISlaService
{
    private readonly IRepository<Complaint> _complaintRepo;
    private readonly INotificationService _notificationService;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailNotificationService _email;

    public SlaService(IRepository<Complaint> complaintRepo, INotificationService notificationService,
        IAuditLogService auditLogService, IEmailNotificationService email)
    {
        _complaintRepo = complaintRepo;
        _notificationService = notificationService;
        _auditLogService = auditLogService;
        _email = email;
    }

    public async Task CheckAllSlasAsync()
    {
        var activeComplaints = await _complaintRepo.Query()
            .Include(c => c.Citizen)
            .Include(c => c.Category)
            .Where(c => !c.IsArchived && c.Status != ComplaintStatus.Resolved
                        && c.Status != ComplaintStatus.Closed && c.SlaDeadline.HasValue)
            .ToListAsync();

        foreach (var complaint in activeComplaints)
        {
            var elapsedPercent = GetElapsedPercent(complaint);
            var wasBreached = complaint.IsSlaBreached;

            if (elapsedPercent >= 100 && !wasBreached)
            {
                complaint.IsSlaBreached = true;
                _complaintRepo.Update(complaint);

                await _notificationService.CreateAsync(
                    complaint.CitizenId,
                    "SLA Breached",
                    $"SLA deadline passed for complaint {complaint.ComplaintNumber}",
                    NotificationType.SlaWarning,
                    complaint.Id);

                await _auditLogService.LogAsync("SLA_BREACHED", "Complaint", complaint.Id,
                    null, null, null,
                    $"SLA breached for complaint {complaint.ComplaintNumber}");
            }
            else if (elapsedPercent >= 80 && elapsedPercent < 100)
            {
                await _notificationService.CreateAsync(
                    complaint.CitizenId,
                    "SLA Warning",
                    $"SLA deadline approaching for complaint {complaint.ComplaintNumber}",
                    NotificationType.SlaWarning,
                    complaint.Id);

                if (!complaint.SlaWarningEmailSent && complaint.Citizen?.Email != null)
                {
                    var remaining = GetRemainingTime(complaint);
                    var remainingStr = remaining.Days > 0
                        ? $"{remaining.Days}d {remaining.Hours}h"
                        : $"{remaining.Hours}h {remaining.Minutes}m";

                    await _email.SendSlaWarningAsync(
                        toEmail: complaint.Citizen.Email,
                        citizenName: complaint.Citizen.FullName,
                        complaintNumber: complaint.ComplaintNumber,
                        complaintTitle: complaint.Title,
                        remaining: remainingStr
                    );

                    complaint.SlaWarningEmailSent = true;
                    _complaintRepo.Update(complaint);
                }
            }
        }

        await _complaintRepo.SaveChangesAsync();
    }

    public TimeSpan GetRemainingTime(Complaint complaint)
    {
        if (!complaint.SlaDeadline.HasValue) return TimeSpan.Zero;
        return complaint.SlaDeadline.Value - DateTime.UtcNow;
    }

    public double GetElapsedPercent(Complaint complaint)
    {
        if (!complaint.SlaDeadline.HasValue) return 0;

        var totalDuration = complaint.SlaDeadline.Value - complaint.CreatedAt;
        var elapsed = DateTime.UtcNow - complaint.CreatedAt;

        if (totalDuration.TotalSeconds <= 0) return 100;
        return Math.Min((elapsed.TotalSeconds / totalDuration.TotalSeconds) * 100, 100);
    }

    public string GetSlaStatusColor(Complaint complaint)
    {
        if (complaint.IsSlaBreached) return "red";
        var percent = GetElapsedPercent(complaint);
        return percent switch
        {
            >= 80 => "orange",
            >= 50 => "yellow",
            _ => "green"
        };
    }
}
