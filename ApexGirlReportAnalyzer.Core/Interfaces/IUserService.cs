using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Core.Interfaces;

public interface IUserService
{
    Task<QuotaInfo> GetRemainingQuotaAsync(Guid userId);

    /// <summary>
    /// Check if user has quota available to upload
    /// </summary>
    /// <returns>True if user can upload, false otherwise</returns>
    Task<bool> HasQuotaAsync(Guid userId);

    /// <summary>
    /// Validate user has quota and return result with error message if not
    /// </summary>
    Task<QuotaValidationResult> ValidateQuotaAsync(Guid userId);

    /// <summary>
    /// Validate both user and server quotas when uploading via Discord
    /// </summary>
    /// <param name="userId">User ID making the upload</param>
    /// <param name="discordServerId">Discord server ID (optional)</param>
    /// <returns>Validation result with combined quota info</returns>
    Task<QuotaValidationResult> ValidateQuotaAsync(Guid userId, Guid? discordServerId);
}