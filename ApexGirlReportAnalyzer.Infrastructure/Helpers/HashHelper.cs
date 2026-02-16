using System.Security.Cryptography;

namespace ApexGirlReportAnalyzer.Infrastructure.Helpers;

/// <summary>
/// Helper methods for hashing operations
/// </summary>
public static class HashHelper
{
    /// <summary>
    /// Calculate SHA-256 hash of a base64-encoded image
    /// </summary>
    /// <param name="base64Image">Base64-encoded image data</param>
    /// <returns>Lowercase hex string of the SHA-256 hash</returns>
    public static string CalculateSha256(string base64Image)
    {
        var imageBytes = Convert.FromBase64String(base64Image);
        var hashBytes = SHA256.HashData(imageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }
}
