namespace InterFullMarkt.Infrastructure.Identity;

using System.Security.Cryptography;
using InterFullMarkt.Application.Abstractions;

public class AuthService : IAuthService
{
    private const int SaltSize = 16;
    private const int KeySize = 20;
    private const int Iterations = 10000;

    public string HashPassword(string password)
    {
        byte[] salt = new byte[SaltSize];
        using (var rng = RandomNumberGenerator.Create()) rng.GetBytes(salt);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
        byte[] hashBytes = new byte[SaltSize + KeySize];
        Array.Copy(salt, 0, hashBytes, 0, SaltSize);
        Array.Copy(hash, 0, hashBytes, SaltSize, KeySize);
        return Convert.ToBase64String(hashBytes);
    }

    public bool VerifyPassword(string password, string savedHash)
    {
        if (string.IsNullOrEmpty(savedHash)) return false;
        try
        {
            byte[] hashBytes = Convert.FromBase64String(savedHash);
            if (hashBytes.Length != SaltSize + KeySize) return false;

            byte[] salt = new byte[SaltSize];
            Array.Copy(hashBytes, 0, salt, 0, SaltSize);

            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(password, salt, Iterations, HashAlgorithmName.SHA256, KeySize);
            for (int i = 0; i < KeySize; i++)
            {
                if (hashBytes[i + SaltSize] != hash[i]) return false;
            }
            return true;
        }
        catch
        {
            return false;
        }
    }
}
