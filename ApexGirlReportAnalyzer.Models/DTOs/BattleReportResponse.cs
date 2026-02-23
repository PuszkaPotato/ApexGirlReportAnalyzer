namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Response from OpenAI containing battle report data.
/// Also used to return battle report data to clients.
/// </summary>
public class BattleReportResponse
{
    /// <summary>
    /// Unique ID of the battle report (set when returning stored reports)
    /// </summary>
    public Guid? ReportId { get; set; }

    /// <summary>
    /// Type of battle (e.g., "Arena", "Guild War")
    /// </summary>
    public string BattleType { get; set; } = string.Empty;

    /// <summary>
    /// Date when the battle occurred
    /// </summary>
    public DateTime BattleDate { get; set; }

    /// <summary>
    /// When the report was uploaded (set when returning stored reports)
    /// </summary>
    public DateTime? UploadedAt { get; set; }

    /// <summary>
    /// Player's battle statistics
    /// </summary>
    public BattleSideDto Player { get; set; } = null!;

    /// <summary>
    /// Enemy's battle statistics
    /// </summary>
    public BattleSideDto Enemy { get; set; } = null!;

    /// <summary>
    /// Number of tokens used for OpenAI analysis
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Estimated cost of OpenAI analysis in USD
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// Whether OpenAI flagged this image as invalid (not a battle report)
    /// </summary>
    public bool IsInvalid { get; set; } = false;

    /// <summary>
    /// Reason why the image was flagged as invalid
    /// </summary>
    public string? InvalidReason { get; set; }
}
