using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Models.Entities;

public class TierLimit : BaseEntity
{
    // Data properties
    public TierScope Scope { get; set; }
    public int DailyRequestLimit { get; set; }
    public int MonthlyRequestLimit { get; set; }

    // Relationships
    public Guid TierId { get; set; }
    public Tier Tier { get; set; } = null!;
}