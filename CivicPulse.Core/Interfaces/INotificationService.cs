using CivicPulse.Core.DTOs;
using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Interfaces;

public interface INotificationService
{
    Task CreateAsync(int userId, string title, string body, NotificationType type, int? relatedComplaintId = null);
    Task MarkAsReadAsync(int notificationId, int userId);
    Task MarkAllAsReadAsync(int userId);
    Task<List<NotificationDto>> GetUnreadAsync(int userId);
    Task<PagedResult<NotificationDto>> GetAllForUserAsync(int userId, int page = 1, int pageSize = 20);
    int GetUnreadCount(int userId);
    Task SendEmailNotificationAsync(string toEmail, string subject, string htmlBody);
}
