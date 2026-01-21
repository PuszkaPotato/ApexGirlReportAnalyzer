using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Models.Entities
{
    public class Upload : BaseEntity
    {
        public required string ImageHash { get; set; }
        public PrivacyScope PrivacyScope { get; set; } = PrivacyScope.Public;
        public UploadStatus Status { get; set; }
        public string? FailureReason { get; set; }
        public string OpenAiModel { get; set; } = null!;
        public string PromptVersion { get; set; } = null!;
        public int TokenEstimate { get; set; }
        public decimal? EstimatedCostEuro { get; set; }
        public DateTime? DeletedAt { get; set; }

        // Foreign Keys
        public Guid UserId { get; set; }
        public Guid? DiscordServerId { get; set; }
        public string? DiscordChannelId { get; set; }
        public string? DiscordMessageId { get; set; }

        // Navigation properties
        public User User { get; set; } = null!;
        public DiscordServer? DiscordServer { get; set; }
        public BattleReport? BattleReport { get; set; }

        // Relationships
        public ICollection<AnalyticsEvent> AnalyticsEvents { get; set; } = new List<AnalyticsEvent>();
        public ICollection<ErrorReport> ErrorReports { get; set; } = new List<ErrorReport>();
    }
}
