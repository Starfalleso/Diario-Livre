using System.Security.Cryptography;

namespace DiarioLivre.Core.Services;

/// <summary>
/// Pure domain helper for validating, hashing, and verifying optional local LGPD access PINs.
/// Uses PBKDF2-SHA256 with a 128-bit random salt and constant-time comparison.
/// </summary>
public static class PinHasher
{
    private const int SaltSize = 16;
    private const int KeySize = 32;
    private const int Iterations = 100_000;

    public static bool IsValidPinFormat(string? pin)
    {
        if (string.IsNullOrEmpty(pin) || pin.Length < 4 || pin.Length > 8)
        {
            return false;
        }

        foreach (char c in pin)
        {
            if (c < '0' || c > '9')
            {
                return false;
            }
        }

        return true;
    }

    public static string HashPin(string pin)
    {
        if (!IsValidPinFormat(pin))
        {
            throw new ArgumentException("PIN must contain between 4 and 8 numeric digits.", nameof(pin));
        }

        byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
        byte[] hash = Rfc2898DeriveBytes.Pbkdf2(pin, salt, Iterations, HashAlgorithmName.SHA256, KeySize);

        return $"{Convert.ToBase64String(salt)}:{Convert.ToBase64String(hash)}";
    }

    public static bool VerifyPin(string? pin, string? storedHash)
    {
        if (string.IsNullOrEmpty(pin) || string.IsNullOrWhiteSpace(storedHash))
        {
            return false;
        }

        string[] parts = storedHash.Split(':', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return false;
        }

        try
        {
            byte[] salt = Convert.FromBase64String(parts[0]);
            byte[] expectedHash = Convert.FromBase64String(parts[1]);
            if (salt.Length == 0 || expectedHash.Length == 0)
            {
                return false;
            }

            byte[] actualHash = Rfc2898DeriveBytes.Pbkdf2(
                pin,
                salt,
                Iterations,
                HashAlgorithmName.SHA256,
                expectedHash.Length);

            return CryptographicOperations.FixedTimeEquals(actualHash, expectedHash);
        }
        catch (FormatException)
        {
            return false;
        }
    }
}
