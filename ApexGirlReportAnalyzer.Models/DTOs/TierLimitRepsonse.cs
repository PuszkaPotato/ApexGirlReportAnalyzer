namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Represents a tier limit in API responses
/// </summary>
public class TierLimitResponse
{
    public string Scope { get; set; } = string.Empty;
    public int DailyRequestLimit { get; set; }
    public int MonthlyRequestLimit { get; set; }
}