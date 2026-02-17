namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Response from the API containing requested battle report data.
/// </summary>
public class BattleReportListResponse
{
    public BattleReportFilterInfo? FiltersApplied { get; set; }
    public int TotalCount { get; set; }
    public int Count { get; set; }
    public List<BattleReportResponse> BattleReports { get; set; } = new List<BattleReportResponse>();
}

public class BattleReportFilterInfo
{
    public Guid? UploadId { get; set; }
    public DateTime? BattleDate { get; set; }
    public string? BattleType { get; set; }
    public Guid? UserId { get; set; }
    public string? Participant { get; set; }
    public string? InGameId { get; set; }
    public string? GroupTag { get; set; }
}
