using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;

namespace CivicPulse.Web.Services;

public class NotificationService : INotificationService
{
    private readonly IRepository<Notification> _notificationRepo;
    private readonly IConfiguration _configuration;

    public NotificationService(IRepository<Notification> notificationRepo, IConfiguration configuration)
    {
        _notificationRepo = notificationRepo;
        _configuration = configuration;
    }

    public async Task CreateAsync(int userId, string title, string body, NotificationType type, int? relatedComplaintId = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            TitleUrdu = title,
            Body = body,
            BodyUrdu = body,
            Type = type,
            RelatedComplaintId = relatedComplaintId,
            IsRead = false
        };

        await _notificationRepo.AddAsync(notification);
        await _notificationRepo.SaveChangesAsync();
    }

    public async Task MarkAsReadAsync(int notificationId, int userId)
    {
        var notification = await _notificationRepo
            .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId);

        if (notification != null)
        {
            notification.IsRead = true;
            _notificationRepo.Update(notification);
            await _notificationRepo.SaveChangesAsync();
        }
    }

    public async Task MarkAllAsReadAsync(int userId)
    {
        var unread = await _notificationRepo.FindAsync(n => n.UserId == userId && !n.IsRead);
        foreach (var n in unread) n.IsRead = true;
        await _notificationRepo.SaveChangesAsync();
    }

    public async Task<List<NotificationDto>> GetUnreadAsync(int userId)
    {
        var notifications = await _notificationRepo.Query()
            .Where(n => n.UserId == userId && !n.IsRead)
            .OrderByDescending(n => n.CreatedAt)
            .Take(10)
            .ToListAsync();

        return notifications.Select(MapToDto).ToList();
    }

    public async Task<PagedResult<NotificationDto>> GetAllForUserAsync(int userId, int page = 1, int pageSize = 20)
    {
        var query = _notificationRepo.Query()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt);

        var total = await query.CountAsync();
        var notifications = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<NotificationDto>
        {
            Items = notifications.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public int GetUnreadCount(int userId)
    {
        return _notificationRepo.Query().Count(n => n.UserId == userId && !n.IsRead);
    }

    public async Task SendEmailNotificationAsync(string toEmail, string subject, string htmlBody)
    {
        var smtpEnabled = _configuration.GetValue<bool>("SmtpSettings:IsEnabled");
        if (!smtpEnabled) return;

        try
        {
            using var client = new System.Net.Mail.SmtpClient
            {
                Host = _configuration["SmtpSettings:Host"] ?? "smtp.gmail.com",
                Port = _configuration.GetValue<int>("SmtpSettings:Port", 587),
                EnableSsl = _configuration.GetValue<bool>("SmtpSettings:EnableSsl", true),
                Credentials = new System.Net.NetworkCredential(
                    _configuration["SmtpSettings:Username"],
                    _configuration["SmtpSettings:Password"])
            };

            var mailMessage = new System.Net.Mail.MailMessage
            {
                From = new System.Net.Mail.MailAddress(
                    _configuration["SmtpSettings:FromEmail"] ?? "noreply@civicpulse.pk",
                    _configuration["SmtpSettings:FromName"] ?? "Civic Pulse"),
                Subject = subject,
                Body = htmlBody,
                IsBodyHtml = true
            };
            mailMessage.To.Add(toEmail);

            await client.SendMailAsync(mailMessage);
        }
        catch
        {
            // Silently fail if SMTP is not configured
        }
    }

    private static NotificationDto MapToDto(Notification n) => new()
    {
        Id = n.Id,
        Title = n.Title,
        Body = n.Body,
        Type = n.Type.ToString(),
        IsRead = n.IsRead,
        RelatedComplaintId = n.RelatedComplaintId,
        CreatedAt = n.CreatedAt
    };
}
