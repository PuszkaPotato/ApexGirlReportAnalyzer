namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Represents a tier with its limits in API responses
/// </summary>
public class TierResponse
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<TierLimitResponse> Limits { get; set; } = new();
}