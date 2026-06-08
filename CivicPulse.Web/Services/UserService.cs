using System.Text.Json;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Entities;
using CivicPulse.Core.Enums;
using CivicPulse.Core.Interfaces;
using Microsoft.Extensions.Configuration;
using CivicPulse.Core.Helpers;

namespace CivicPulse.Web.Services;

public class UserService : IUserService
{
    private readonly IRepository<ApplicationUser> _userRepo;
    private readonly IConfiguration _configuration;
    private readonly IAuditLogService _auditLogService;
    private readonly IEmailNotificationService _email;

    public UserService(IRepository<ApplicationUser> userRepo,
        IConfiguration configuration, IAuditLogService auditLogService,
        IEmailNotificationService email)
    {
        _userRepo = userRepo;
        _configuration = configuration;
        _auditLogService = auditLogService;
        _email = email;
    }

    public async Task<UserDto> RegisterAsync(RegisterDto dto)
    {
        var existing = await _userRepo.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (existing != null) throw new InvalidOperationException("Email already registered.");

        if (dto.Password != dto.ConfirmPassword) throw new InvalidOperationException("Passwords do not match.");

        var user = new ApplicationUser
        {
            FullName = dto.FullName,
            Email = dto.Email,
            PhoneNumber = dto.PhoneNumber,
            PasswordHash = PasswordHelper.HashPassword(dto.Password),
            Role = UserRole.Citizen,
            IsActive = true,
            IsEmailVerified = true
        };

        await _userRepo.AddAsync(user);
        await _userRepo.SaveChangesAsync();

        // Send Welcome Email
        await _email.SendRawAsync(
            toEmail: user.Email,
            subject: "Welcome to Civic Pulse",
            htmlBody: $"<p>Hi {user.FullName},</p><p>Thank you for registering on Civic Pulse!</p><p>You can now log in and submit complaints.</p>"
        );

        return MapToDto(user);
    }

    public async Task<UserDto> LoginAsync(LoginDto dto)
    {
        var user = await _userRepo.FirstOrDefaultAsync(u => u.Email == dto.Email);
        if (user == null) throw new UnauthorizedAccessException("Invalid email or password.");

        if (!user.IsActive) throw new UnauthorizedAccessException("Account is disabled. Contact administrator.");

        if (user.LockoutEndAt.HasValue && user.LockoutEndAt > DateTime.UtcNow)
            throw new UnauthorizedAccessException("Account locked. Try again later.");

        if (!PasswordHelper.VerifyPassword(dto.Password, user.PasswordHash))
        {
            user.FailedLoginAttempts++;
            if (user.FailedLoginAttempts >= _configuration.GetValue<int>("AppSettings:MaxLoginAttempts", 5))
            {
                user.LockoutEndAt = DateTime.UtcNow.AddMinutes(_configuration.GetValue<int>("AppSettings:LockoutMinutes", 15));
            }
            await _userRepo.SaveChangesAsync();
            throw new UnauthorizedAccessException("Invalid email or password.");
        }

        user.FailedLoginAttempts = 0;
        user.LockoutEndAt = null;
        user.LastLoginAt = DateTime.UtcNow;
        await _userRepo.SaveChangesAsync();

        return MapToDto(user);
    }

    public async Task<UserDto> GetByIdAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");
        return MapToDto(user);
    }

    public async Task<UserDto> GetByEmailAsync(string email)
    {
        var user = await _userRepo.FirstOrDefaultAsync(u => u.Email == email)
            ?? throw new KeyNotFoundException("User not found.");
        return MapToDto(user);
    }

    public async Task<PagedResult<UserDto>> GetAllAsync(int page = 1, int pageSize = 20)
    {
        var query = _userRepo.Query().OrderByDescending(u => u.CreatedAt);
        var total = await _userRepo.CountAsync();
        var users = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<UserDto> UpdateProfileAsync(int id, string fullName, string phoneNumber)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        var oldValues = JsonSerializer.Serialize(new { user.FullName, user.PhoneNumber });
        user.FullName = fullName;
        user.PhoneNumber = phoneNumber;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        await _auditLogService.LogAsync("PROFILE_UPDATED", "User", id, oldValues,
            JsonSerializer.Serialize(new { fullName, phoneNumber }), id, "User updated profile");

        return MapToDto(user);
    }

    public async Task<bool> ChangePasswordAsync(int id, string currentPassword, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        if (!PasswordHelper.VerifyPassword(currentPassword, user.PasswordHash))
            throw new InvalidOperationException("Current password is incorrect.");

        user.            PasswordHash = PasswordHelper.HashPassword(newPassword);
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        await _auditLogService.LogAsync("PASSWORD_CHANGED", "User", id, null, null, id, "User changed password");
        return true;
    }

    public async Task<bool> ToggleUserStatusAsync(int id)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        user.IsActive = !user.IsActive;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        await _auditLogService.LogAsync("USER_STATUS_TOGGLED", "User", id,
            JsonSerializer.Serialize(new { IsActive = !user.IsActive }),
            JsonSerializer.Serialize(new { IsActive = user.IsActive }),
            null, $"User {(user.IsActive ? "enabled" : "disabled")}");

        return user.IsActive;
    }

    public async Task<bool> UpdateRoleAsync(int id, string role)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        var oldRole = user.Role.ToString();
        user.Role = Enum.Parse<UserRole>(role);
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        await _auditLogService.LogAsync("ROLE_CHANGED", "User", id,
            JsonSerializer.Serialize(new { Role = oldRole }),
            JsonSerializer.Serialize(new { Role = role }),
            null, $"User role changed from {oldRole} to {role}");

        return true;
    }

    public async Task<bool> ResetPasswordAsync(int id, string newPassword)
    {
        var user = await _userRepo.GetByIdAsync(id)
            ?? throw new KeyNotFoundException("User not found.");

        user.            PasswordHash = PasswordHelper.HashPassword(newPassword);
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        await _auditLogService.LogAsync("PASSWORD_RESET", "User", id, null, null, null, "Admin reset user password");
        return true;
    }

    public async Task<PagedResult<UserDto>> GetFilteredAsync(string? role, bool? isActive, string? search, int page = 1, int pageSize = 20)
    {
        var query = _userRepo.Query().AsQueryable();

        if (!string.IsNullOrEmpty(role) && Enum.TryParse<UserRole>(role, out var userRole))
            query = query.Where(u => u.Role == userRole);

        if (isActive.HasValue)
            query = query.Where(u => u.IsActive == isActive.Value);

        if (!string.IsNullOrEmpty(search))
            query = query.Where(u => u.FullName.Contains(search) || u.Email.Contains(search));

        var total = await query.CountAsync();
        var users = await query.OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();

        return new PagedResult<UserDto>
        {
            Items = users.Select(MapToDto).ToList(),
            TotalCount = total,
            Page = page,
            PageSize = pageSize
        };
    }

    public async Task<bool> InitiatePasswordResetAsync(string email)
    {
        var user = await _userRepo.FirstOrDefaultAsync(
            u => u.Email.ToLower() == email.ToLower());

        if (user == null) return true;

        var token = Convert.ToBase64String(
            System.Security.Cryptography.RandomNumberGenerator.GetBytes(32))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");

        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        await _email.SendPasswordResetAsync(
            toEmail: user.Email,
            userName: user.FullName,
            resetToken: token
        );

        await _auditLogService.LogAsync(
            "PASSWORD_RESET_REQUESTED", "User", user.Id,
            null, null, null,
            $"Password reset requested for {user.Email}");

        return true;
    }

    public async Task<bool> ValidateResetTokenAsync(string token)
    {
        var user = await _userRepo.FirstOrDefaultAsync(
            u => u.PasswordResetToken == token &&
                 u.PasswordResetTokenExpiry > DateTime.UtcNow);

        return user != null;
    }

    public async Task<bool> ResetPasswordByTokenAsync(string token, string newPassword)
    {
        var user = await _userRepo.FirstOrDefaultAsync(
            u => u.PasswordResetToken == token &&
                 u.PasswordResetTokenExpiry > DateTime.UtcNow);

        if (user == null) return false;
        if (newPassword.Length < 8) return false;

        user.PasswordHash = PasswordHelper.HashPassword(newPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        _userRepo.Update(user);
        await _userRepo.SaveChangesAsync();

        await _auditLogService.LogAsync(
            "PASSWORD_RESET_COMPLETED", "User", user.Id,
            null, null, user.Id,
            $"Password reset completed for {user.Email}");

        return true;
    }

    private UserDto MapToDto(ApplicationUser user) => new()
    {
        Id = user.Id,
        FullName = user.FullName,
        Email = user.Email,
        PhoneNumber = user.PhoneNumber,
        CNIC = user.CNIC,
        Role = user.Role,
        RoleName = user.Role.ToString(),
        IsActive = user.IsActive,
        IsEmailVerified = user.IsEmailVerified,
        LastLoginAt = user.LastLoginAt,
        CreatedAt = user.CreatedAt,
        ComplaintCount = user.Complaints?.Count ?? 0
    };
}
