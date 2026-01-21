namespace ApexGirlReportAnalyzer.Models.Entities
{
    public class BattleReport : BaseEntity
    {
        public int ExtractionVersion { get; set; }
        public DateTime BattleDate { get; set; }
        public string BattleType { get; set; } = string.Empty;
        public DateTime? DeletedAt { get; set; }

        // Foreign Keys
        public Guid UploadId { get; set; }

        // Navigation properties
        public Upload Upload { get; set; } = null!;

        // Relationships
        public ICollection<BattleSide> BattleSides { get; set; } = new List<BattleSide>();
    }
}