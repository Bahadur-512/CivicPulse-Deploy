using CivicPulse.Core.Enums;

namespace CivicPulse.Core.Entities;

public class ApplicationUser : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string PhoneNumber { get; set; } = string.Empty;
    public string CNIC { get; set; } = string.Empty;
    public string ProfileImagePath { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Citizen;
    public bool IsActive { get; set; } = true;
    public bool IsEmailVerified { get; set; }
    public string EmailVerificationToken { get; set; } = string.Empty;
    public DateTime? LastLoginAt { get; set; }
    public int FailedLoginAttempts { get; set; }
    public DateTime? LockoutEndAt { get; set; }
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public ICollection<Complaint> Complaints { get; set; } = new List<Complaint>();
    public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    public ICollection<AuditLog> AuditLogs { get; set; } = new List<AuditLog>();
    public ICollection<Feedback> Feedbacks { get; set; } = new List<Feedback>();
}
