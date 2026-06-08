using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Configuration;

namespace CivicPulse.Web.Services;

public class SmtpSettings
{
    public string Host { get; set; } = "smtp.gmail.com";
    public int Port { get; set; } = 587;
    public string Username { get; set; } = "";
    public string Password { get; set; } = "";
    public string FromEmail { get; set; } = "";
    public string FromName { get; set; } = "Civic Pulse";
    public bool EnableSsl { get; set; } = true;
    public bool IsEnabled { get; set; } = false;
}

public interface IEmailNotificationService
{
    Task SendStatusUpdateAsync(string toEmail, string citizenName,
        string complaintNumber, string complaintTitle,
        string oldStatus, string newStatus, string? note = null);

    Task SendComplaintSubmittedAsync(string toEmail, string citizenName,
        string complaintNumber, string complaintTitle, string category);

    Task SendSlaWarningAsync(string toEmail, string citizenName,
        string complaintNumber, string complaintTitle, string remaining);

    Task SendFeedbackRequestAsync(string toEmail, string citizenName,
        string complaintNumber, int complaintId);

    Task SendPasswordResetAsync(string toEmail, string userName, string resetToken);
    Task SendRawAsync(string toEmail, string subject, string htmlBody);
}

public class EmailNotificationService : IEmailNotificationService
{
    private readonly SmtpSettings _smtp;
    private readonly IConfiguration _config;
    private readonly ILogger<EmailNotificationService> _logger;

    public EmailNotificationService(
        IOptions<SmtpSettings> smtp,
        IConfiguration config,
        ILogger<EmailNotificationService> logger)
    {
        _smtp = smtp.Value;
        _config = config;
        _logger = logger;
    }

    private string AppUrl =>
        _config["AppSettings:AppUrl"] ?? "https://localhost:7150";

    // ── STATUS UPDATE EMAIL ──
    public async Task SendStatusUpdateAsync(
        string toEmail, string citizenName,
        string complaintNumber, string complaintTitle,
        string oldStatus, string newStatus, string? note = null)
    {
        var statusColor = newStatus switch
        {
            "Resolved"    => "#16A34A",
            "InProgress"  => "#2563EB",
            "UnderReview" => "#3B82F6",
            "Rejected"    => "#DC2626",
            "Closed"      => "#6B7280",
            _             => "#D97706"
        };

        var statusLabel = newStatus switch
        {
            "UnderReview" => "Under Review",
            "InProgress"  => "In Progress",
            _             => newStatus
        };

        var html = BuildEmailTemplate(
            title: "Complaint Status Updated",
            citizenName: citizenName,
            body: $@"
            <p style='color:#374151;font-size:15px;margin:0 0 20px;'>
                Your complaint <strong>{complaintNumber}</strong> has been updated.
            </p>
            <div style='background:#F8FAFC;border-radius:10px;padding:16px;margin-bottom:20px;'>
                <p style='color:#6B7280;font-size:13px;margin:0 0 8px;'>COMPLAINT</p>
                <p style='color:#1A202C;font-weight:600;font-size:15px;margin:0;'>{complaintTitle}</p>
            </div>
            <div style='display:flex;gap:12px;margin-bottom:20px;'>
                <div style='flex:1;background:#FEF2F2;border-radius:8px;padding:12px;text-align:center;'>
                    <p style='color:#9CA3AF;font-size:11px;margin:0 0 4px;text-transform:uppercase;'>FROM</p>
                    <p style='color:#374151;font-weight:600;font-size:14px;margin:0;'>{oldStatus}</p>
                </div>
                <div style='padding-top:16px;color:#9CA3AF;font-size:20px;'>→</div>
                <div style='flex:1;background:{statusColor}18;border-radius:8px;padding:12px;text-align:center;border:2px solid {statusColor}40;'>
                    <p style='color:#9CA3AF;font-size:11px;margin:0 0 4px;text-transform:uppercase;'>TO</p>
                    <p style='color:{statusColor};font-weight:700;font-size:14px;margin:0;'>{statusLabel}</p>
                </div>
            </div>
            {(note != null ? $"<div style='background:#EFF6FF;border-left:4px solid #1B3A6B;padding:12px 16px;border-radius:0 8px 8px 0;margin-bottom:20px;'><p style='color:#1B3A6B;font-size:13px;margin:0;font-style:italic;'>Note: {note}</p></div>" : "")}",
            actionUrl: $"{AppUrl}/citizen/complaints",
            actionText: "View My Complaints"
        );

        await SendRawAsync(toEmail,
            $"Complaint {complaintNumber} — Status Updated to {statusLabel}",
            html);
    }

    // ── COMPLAINT SUBMITTED EMAIL ──
    public async Task SendComplaintSubmittedAsync(
        string toEmail, string citizenName,
        string complaintNumber, string complaintTitle, string category)
    {
        var html = BuildEmailTemplate(
            title: "Complaint Submitted Successfully",
            citizenName: citizenName,
            body: $@"
            <p style='color:#374151;font-size:15px;margin:0 0 20px;'>
                Your complaint has been received and is now under review.
            </p>
            <div style='background:#F0FDF4;border:1.5px solid #86EFAC;border-radius:10px;padding:20px;margin-bottom:20px;'>
                <div style='display:flex;align-items:center;gap:10px;margin-bottom:12px;'>
                    <span style='font-size:24px;'>✅</span>
                    <div>
                        <p style='color:#15803D;font-weight:800;font-size:16px;margin:0;'>{complaintNumber}</p>
                        <p style='color:#166534;font-size:13px;margin:0;'>Successfully submitted</p>
                    </div>
                </div>
                <p style='color:#1A202C;font-weight:600;font-size:14px;margin:0 0 6px;'>{complaintTitle}</p>
                <span style='background:#16A34A18;color:#15803D;font-size:12px;font-weight:600;padding:3px 10px;border-radius:20px;'>{category}</span>
            </div>
            <p style='color:#6B7280;font-size:14px;'>
                You will receive email updates when the status of your complaint changes.
                You can also track it anytime in your Civic Pulse dashboard.
            </p>",
            actionUrl: $"{AppUrl}/citizen/complaints",
            actionText: "Track My Complaint"
        );

        await SendRawAsync(toEmail,
            $"✅ Complaint Submitted — {complaintNumber}",
            html);
    }

    // ── SLA WARNING EMAIL ──
    public async Task SendSlaWarningAsync(
        string toEmail, string citizenName,
        string complaintNumber, string complaintTitle, string remaining)
    {
        var html = BuildEmailTemplate(
            title: "SLA Deadline Approaching",
            citizenName: citizenName,
            body: $@"
            <div style='background:#FEF9C3;border:1.5px solid #FDE68A;border-radius:10px;padding:20px;margin-bottom:20px;'>
                <div style='display:flex;align-items:center;gap:10px;margin-bottom:8px;'>
                    <span style='font-size:24px;'>⚠️</span>
                    <p style='color:#92400E;font-weight:700;font-size:15px;margin:0;'>Deadline Approaching</p>
                </div>
                <p style='color:#78350F;font-size:14px;margin:0;'>
                    Your complaint <strong>{complaintNumber}</strong> has <strong>{remaining}</strong> remaining before its SLA deadline.
                </p>
            </div>
            <p style='color:#374151;font-size:14px;'>
                <strong>{complaintTitle}</strong> — Our team is working on resolving this issue. 
                If you need urgent attention, please contact support.
            </p>",
            actionUrl: $"{AppUrl}/citizen/complaints",
            actionText: "View Complaint"
        );

        await SendRawAsync(toEmail,
            $"⚠️ SLA Deadline Approaching — {complaintNumber}",
            html);
    }

    // ── FEEDBACK REQUEST EMAIL ──
    public async Task SendFeedbackRequestAsync(
        string toEmail, string citizenName,
        string complaintNumber, int complaintId)
    {
        var html = BuildEmailTemplate(
            title: "How was your experience?",
            citizenName: citizenName,
            body: $@"
            <p style='color:#374151;font-size:15px;margin:0 0 16px;'>
                Your complaint <strong>{complaintNumber}</strong> has been resolved! 🎉
            </p>
            <p style='color:#374151;font-size:14px;margin:0 0 20px;'>
                We'd love to hear your feedback. Your rating helps us improve 
                urban services for everyone in the city.
            </p>
            <div style='text-align:center;font-size:32px;letter-spacing:8px;margin-bottom:20px;'>
                ⭐⭐⭐⭐⭐
            </div>",
            actionUrl: $"{AppUrl}/citizen/feedback/{complaintId}",
            actionText: "Rate Your Experience"
        );

        await SendRawAsync(toEmail,
            $"⭐ How was your experience? Rate complaint {complaintNumber}",
            html);
    }

    // ── PASSWORD RESET EMAIL ──
    public async Task SendPasswordResetAsync(
        string toEmail, string userName, string resetToken)
    {
        var resetUrl = $"{AppUrl}/reset-password?token={resetToken}";

        var html = BuildEmailTemplate(
            title: "Reset Your Password",
            citizenName: userName,
            body: $@"
        <p style='color:#374151;font-size:15px;margin:0 0 16px;'>
            We received a request to reset your Civic Pulse password.
            Click the button below to set a new password.
        </p>
        <div style='background:#FEF9C3;border:1px solid #FDE68A;border-radius:10px;
                    padding:14px 18px;margin-bottom:20px;'>
            <p style='color:#92400E;font-size:13px;margin:0;'>
                ⏰ This link expires in <strong>1 hour</strong>.
                If you did not request a password reset, ignore this email.
            </p>
        </div>",
            actionUrl: resetUrl,
            actionText: "Reset My Password"
        );

        await SendRawAsync(toEmail,
            "🔐 Reset Your Civic Pulse Password",
            html);
    }

    // ── SEND RAW HTML EMAIL ──
    public Task SendRawAsync(string toEmail, string subject, string htmlBody)
    {
        if (!_smtp.IsEnabled)
        {
            _logger.LogInformation("Email skipped (disabled): {subject} → {email}", subject, toEmail);
            return Task.CompletedTask;
        }

        // Fire and forget so we don't block the UI if SMTP times out
        _ = Task.Run(async () =>
        {
            try
            {
                var message = new MimeMessage();
                message.From.Add(new MailboxAddress(_smtp.FromName, _smtp.FromEmail));
                message.To.Add(new MailboxAddress("", toEmail));
                message.Subject = subject;
                message.Body = new TextPart("html") { Text = htmlBody };

                using var client = new SmtpClient();
                await client.ConnectAsync(_smtp.Host, _smtp.Port, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_smtp.Username, _smtp.Password);
                await client.SendAsync(message);
                await client.DisconnectAsync(true);

                _logger.LogInformation("Email sent: {subject} → {email}", subject, toEmail);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {email}: {subject}", toEmail, subject);
            }
        });
        
        return Task.CompletedTask;
    }

    // ── EMAIL TEMPLATE ──
    private string BuildEmailTemplate(
        string title, string citizenName,
        string body, string actionUrl, string actionText)
    {
        return $@"<!DOCTYPE html>
<html>
<head><meta charset='utf-8'><meta name='viewport' content='width=device-width'></head>
<body style='margin:0;padding:0;background:#F0F4F8;font-family:Arial,sans-serif;'>
    <table width='100%' cellpadding='0' cellspacing='0' style='background:#F0F4F8;padding:32px 0;'>
        <tr><td align='center'>
            <table width='580' cellpadding='0' cellspacing='0' style='max-width:580px;width:100%;'>

                <!-- Header -->
                <tr><td style='background:linear-gradient(135deg,#0D2040,#1B3A6B);padding:28px 32px;border-radius:16px 16px 0 0;'>
                    <table width='100%'><tr>
                        <td>
                            <p style='color:#fff;font-size:20px;font-weight:800;margin:0;'>⚡ Civic Pulse</p>
                            <p style='color:rgba(255,255,255,0.5);font-size:12px;margin:4px 0 0;text-transform:uppercase;letter-spacing:1px;'>Urban Complaint Management</p>
                        </td>
                    </tr></table>
                </td></tr>

                <!-- Body -->
                <tr><td style='background:#ffffff;padding:32px;'>
                    <h1 style='color:#1A202C;font-size:22px;font-weight:800;margin:0 0 8px;'>{title}</h1>
                    <p style='color:#6B7280;font-size:14px;margin:0 0 24px;'>Hello <strong>{citizenName}</strong>,</p>
                    {body}
                    <div style='text-align:center;margin:28px 0 0;'>
                        <a href='{actionUrl}'
                           style='display:inline-block;background:linear-gradient(135deg,#1B3A6B,#2D5BA3);color:#fff;
                                  text-decoration:none;padding:13px 28px;border-radius:10px;
                                  font-weight:700;font-size:14px;'>
                            {actionText} →
                        </a>
                    </div>
                </td></tr>

                <!-- Footer -->
                <tr><td style='background:#F8FAFC;padding:20px 32px;border-radius:0 0 16px 16px;border-top:1px solid #E2E8F0;'>
                    <p style='color:#9CA3AF;font-size:12px;margin:0;text-align:center;'>
                        This email was sent by Civic Pulse — Automated Notification<br>
                        You received this because you registered a complaint on our platform.
                    </p>
                </td></tr>

            </table>
        </td></tr>
    </table>
</body>
</html>";
    }
}
