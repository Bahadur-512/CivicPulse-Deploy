namespace CivicPulse.Core.Models;

public class JwtSettings
{
    public string SecretKey { get; set; } = "";
    public string Issuer { get; set; } = "CivicPulse";
    public string Audience { get; set; } = "CivicPulseUsers";
    public int ExpiryHours { get; set; } = 24;
}