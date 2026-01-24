namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Response from OpenAI containing battle report data
/// Also used to return battle report data to clients
/// </summary>
public class BattleReportResponse
{
    public bool IsInvalid { get; set; }
    public string? InvalidReason { get; set; }
    public string BattleType { get; set; } = string.Empty;
    public DateTime BattleDate { get; set; }

    public BattleSideDto Player { get; set; } = new();
    public BattleSideDto Enemy { get; set; } = new();

    // Metadata about the analysis
    public int? TokensUsed { get; set; }
    public decimal? EstimatedCost { get; set; }
}