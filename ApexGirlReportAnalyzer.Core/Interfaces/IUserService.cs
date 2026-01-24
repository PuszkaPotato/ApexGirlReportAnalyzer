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
    
    // OR even better:
    
    /// <summary>
    /// Validate user has quota and return result with error message if not
    /// </summary>
    Task<QuotaValidationResult> ValidateQuotaAsync(Guid userId);
}