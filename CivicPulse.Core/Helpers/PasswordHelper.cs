using System.Security.Cryptography;

namespace CivicPulse.Core.Helpers;

public static class PasswordHelper
{
    private const int SaltSize = 16;
    private const int HashSize = 32;
    private const int Iterations = 100000;

    public static string HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(SaltSize);
        var hash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        var result = new byte[SaltSize + HashSize];
        Buffer.BlockCopy(salt, 0, result, 0, SaltSize);
        Buffer.BlockCopy(hash, 0, result, SaltSize, HashSize);

        return Convert.ToBase64String(result);
    }

    public static bool VerifyPassword(string password, string hashedPassword)
    {
        var hashBytes = Convert.FromBase64String(hashedPassword);
        var salt = hashBytes[..SaltSize];
        var storedHash = hashBytes[SaltSize..];

        var computedHash = Rfc2898DeriveBytes.Pbkdf2(
            password, salt, Iterations, HashAlgorithmName.SHA256, HashSize);

        return CryptographicOperations.FixedTimeEquals(storedHash, computedHash);
    }

    public static string GenerateTempPassword(int length = 12)
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghjkmnpqrstuvwxyz23456789!@#$%";
        return string.Create(length, RandomNumberGenerator.GetBytes(length), (span, bytes) =>
        {
            for (int i = 0; i < length; i++)
                span[i] = chars[bytes[i] % chars.Length];
        });
    }
}
