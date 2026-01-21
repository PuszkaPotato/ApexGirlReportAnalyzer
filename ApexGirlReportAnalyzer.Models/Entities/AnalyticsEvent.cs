using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Models.Entities
{
    public class AnalyticsEvent : BaseEntity
    {
        public AnalyticsEventType EventType { get; set; }
        public string? Metadata { get; set; }

        // Foreign Keys
        public Guid UserId { get; set; }
        public Guid? DiscordServerId { get; set; }
        public Guid? UploadId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public DiscordServer? DiscordServer { get; set; }
        public Upload? Upload { get; set; }
    }
}
