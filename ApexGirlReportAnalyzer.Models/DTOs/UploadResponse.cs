using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Response model for screenshot upload
/// </summary>
public class UploadResponse
{
    /// <summary>
    /// Indicates if the upload was successful
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Unique ID of the upload record
    /// </summary>
    public Guid? UploadId { get; set; }

    /// <summary>
    /// Status of the upload (Pending, Success, Failed)
    /// </summary>
    public UploadStatus Status { get; set; }

    /// <summary>
    /// The extracted battle report data (if successful)
    /// </summary>
    public BattleReportResponse? BattleData { get; set; }

    /// <summary>
    /// Whether this was a duplicate upload (already processed)
    /// </summary>
    public bool IsDuplicate { get; set; }

    /// <summary>
    /// If duplicate, the ID of the existing battle report
    /// </summary>
    public Guid? ExistingBattleReportId { get; set; }

    /// <summary>
    /// Error message if upload failed
    /// </summary>
    public string? ErrorMessage { get; set; }

    /// <summary>
    /// Tokens used for this analysis
    /// </summary>
    public int? TokensUsed { get; set; }

    /// <summary>
    /// Estimated cost for this analysis
    /// </summary>
    public decimal? EstimatedCost { get; set; }

    /// <summary>
    /// User's remaining quota (uploads left for today/month)
    /// </summary>
    public QuotaInfo? RemainingQuota { get; set; }
}

/// <summary>
/// Information about user's remaining quota
/// </summary>
public class QuotaInfo
{
    public int DailyRemaining { get; set; }
    public int MonthlyRemaining { get; set; }
    public string TierName { get; set; } = string.Empty;

    // Server quota (only populated when uploading via Discord server)
    public int? ServerDailyRemaining { get; set; }
    public int? ServerMonthlyRemaining { get; set; }
    public string? ServerTierName { get; set; }
}