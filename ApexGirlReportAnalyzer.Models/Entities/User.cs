using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Models.Entities;

public class User : BaseEntity
{
    // Regular properties (columns)
    public string DiscordId { get; set; } = string.Empty;
    public string? InGamePlayerId { get; set; }  // Nullable
    public DateTime? DeletedAt { get; set; }     // Soft delete

    // Foreign Key (belongs to one Tier)
    public Guid TierId { get; set; }

    // Navigation Properties
    // One-to-one/many-to-one (singular)
    public Tier Tier { get; set; } = null!;  // null! means "trust me, EF Core will set this"

    // One-to-many (collections)
    public ICollection<Upload> Uploads { get; set; } = new List<Upload>();
    public ICollection<AnalyticsEvent> AnalyticsEvents { get; set; } = new List<AnalyticsEvent>();
    public ICollection<ErrorReport> ErrorReports { get; set; } = new List<ErrorReport>();
}