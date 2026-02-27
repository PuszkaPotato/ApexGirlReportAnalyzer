namespace ApexGirlReportAnalyzer.Models.DTOs;

public class CreateTierRequest
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public TierLimitRequest? UserLimit { get; set; }
    public TierLimitRequest? ServerLimit { get; set; }
}