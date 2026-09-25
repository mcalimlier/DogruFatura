using System.Security.Cryptography;

namespace DogruFatura.Core.Services;

public static class FileIntegrityService
{
    public static string ComputeSha256(string filePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(filePath);
        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    public static bool IsValidSha256(string? value)
    {
        if (string.IsNullOrWhiteSpace(value) || value.Length != 64) return false;
        return value.All(Uri.IsHexDigit);
    }

    public static bool MatchesSha256(string filePath, string expectedSha256) =>
        IsValidSha256(expectedSha256)
        && string.Equals(
            ComputeSha256(filePath),
            expectedSha256,
            StringComparison.OrdinalIgnoreCase);
}
