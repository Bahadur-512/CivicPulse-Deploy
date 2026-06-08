using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;

namespace CivicPulse.Web.Services;

public class JwtAuthStateProvider : AuthenticationStateProvider
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly IJwtTokenService _jwtService;

    public JwtAuthStateProvider(
        IHttpContextAccessor httpContextAccessor,
        IJwtTokenService jwtService)
    {
        _httpContextAccessor = httpContextAccessor;
        _jwtService = jwtService;
    }

    public override Task<AuthenticationState> GetAuthenticationStateAsync()
    {
        var context = _httpContextAccessor.HttpContext;

        var token = context?.Request.Cookies["civic_jwt"];

        if (string.IsNullOrEmpty(token))
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }

        var principal = _jwtService.ValidateToken(token);

        if (principal == null)
        {
            var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
            return Task.FromResult(new AuthenticationState(anonymous));
        }

        return Task.FromResult(new AuthenticationState(principal));
    }

    public void NotifyAuthStateChanged()
    {
        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }
}