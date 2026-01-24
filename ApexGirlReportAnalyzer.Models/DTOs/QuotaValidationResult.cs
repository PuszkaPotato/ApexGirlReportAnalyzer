namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Result of quota validation check
/// </summary>
public class QuotaValidationResult
{
    /// <summary>
    /// Whether the user has available quota
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Current quota information
    /// </summary>
    public QuotaInfo QuotaInfo { get; set; } = new();

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; set; }
}