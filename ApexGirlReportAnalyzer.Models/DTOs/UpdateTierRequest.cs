namespace ApexGirlReportAnalyzer.Models.DTOs;

public class UpdateTierRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false;
    public TierLimitRequest? UserLimit { get; set; }
    public TierLimitRequest? ServerLimit { get; set; }
}