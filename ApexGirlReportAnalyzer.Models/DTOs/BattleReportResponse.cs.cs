namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Response from OpenAI with extracted battle report data
/// </summary>
public class BattleReportResponse
{
    public string BattleType { get; set; } = string.Empty;
    public DateTime BattleDate { get; set; }
    public BattleSideResponse Player { get; set; } = new();
    public BattleSideResponse Enemy { get; set; } = new();

    // Metadata from OpenAI
    public int TokensUsed { get; set; }
    public decimal EstimatedCost { get; set; }
}