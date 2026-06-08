using CivicPulse.Core.DTOs;

namespace CivicPulse.Web.Services;

public interface IJwtAuthService
{
    Task SignInAsync(UserDto user);
    Task SignOutAsync();
}

public class JwtAuthService : IJwtAuthService
{
    private readonly IJwtTokenService _jwtService;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public JwtAuthService(
        IJwtTokenService jwtService,
        IHttpContextAccessor httpContextAccessor)
    {
        _jwtService = jwtService;
        _httpContextAccessor = httpContextAccessor;
    }

    public Task SignInAsync(UserDto user)
    {
        var token = _jwtService.GenerateToken(user);

        _httpContextAccessor.HttpContext?.Response.Cookies.Append(
            "civic_jwt",
            token,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Expires = DateTimeOffset.UtcNow.AddHours(24),
                Path = "/"
            });

        return Task.CompletedTask;
    }

    public Task SignOutAsync()
    {
        _httpContextAccessor.HttpContext?.Response.Cookies.Delete("civic_jwt");

        return Task.CompletedTask;
    }
}