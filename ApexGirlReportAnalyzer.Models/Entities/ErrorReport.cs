namespace ApexGirlReportAnalyzer.Models.Entities
{
    public class ErrorReport : BaseEntity
    {
        public required string ReportedIssue { get; set; }
        public string? CorrectedData { get; set; }
        public DateTime? ResolvedAt { get; set; }

        // Foreign Keys
        public Guid UploadId { get; set; }
        public Guid UserId { get; set; }

        // Navigation properties
        public Upload Upload { get; set; } = null!;
        public User User { get; set; } = null!;

    }
}
