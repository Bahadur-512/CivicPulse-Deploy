using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using CivicPulse.Core.DTOs;
using CivicPulse.Core.Models;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace CivicPulse.Web.Services;

public interface IJwtTokenService
{
    string GenerateToken(UserDto user);
    ClaimsPrincipal? ValidateToken(string token);
    string? GetClaimValue(string token, string claimType);
}

public class JwtTokenService : IJwtTokenService
{
    private readonly JwtSettings _settings;

    public JwtTokenService(IOptions<JwtSettings> settings)
    {
        _settings = settings.Value;
    }

    public string GenerateToken(UserDto user)
    {
        var key = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(_settings.SecretKey));

        var credentials = new SigningCredentials(
            key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim("userId",   user.Id.ToString()),
            new Claim("email",    user.Email),
            new Claim("fullName", user.FullName),
            new Claim("role",     user.Role.ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.FullName),
            new Claim(ClaimTypes.Email, user.Email),
        };

        var token = new JwtSecurityToken(
            issuer:   _settings.Issuer,
            audience: _settings.Audience,
            claims:   claims,
            expires:  DateTime.UtcNow.AddHours(_settings.ExpiryHours),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public ClaimsPrincipal? ValidateToken(string token)
    {
        try
        {
            var key = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_settings.SecretKey));

            var handler = new JwtSecurityTokenHandler { MapInboundClaims = false };

            var principal = handler.ValidateToken(token,
                new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = key,
                    ValidateIssuer = true,
                    ValidIssuer = _settings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _settings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                },
                out _);

            return principal;
        }
        catch
        {
            return null;
        }
    }

    public string? GetClaimValue(string token, string claimType)
    {
        try
        {
            var handler = new JwtSecurityTokenHandler();
            var jwt = handler.ReadJwtToken(token);
            return jwt.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
        }
        catch { return null; }
    }
}