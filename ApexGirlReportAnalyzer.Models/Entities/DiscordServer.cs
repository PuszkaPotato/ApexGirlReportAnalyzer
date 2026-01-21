using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Models.Entities
{
    public class DiscordServer : BaseEntity
    {
        // Regular properties (columns in database)
        public string DiscordServerId { get; set; } = string.Empty;
        public string OwnerDiscordId { get; set; } = string.Empty;
        public string ModeratorRoleIds { get; set; } = "[]"; // JSON array stored as string
        public PrivacyScope DefaultReportPrivacy { get; set; } = PrivacyScope.Public;
        public DateTime? DeletedAt { get; set; } // Soft delete

        // Foreign Key (nullable - server might not have tier!)
        public Guid? ServerTierId { get; set; }

        // Navigation properties (relationships)
        public Tier? Tier { get; set; }
        public ICollection<Upload> Uploads { get; set; } = new List<Upload>();
        public ICollection<AnalyticsEvent> AnalyticsEvents { get; set; } = new List<AnalyticsEvent>();
    }
}
