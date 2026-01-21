namespace ApexGirlReportAnalyzer.Models.Entities;

public class ApiKey : BaseEntity
{
    // Regular properties (columns in database)
    public string Key { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Scope { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Nullable DateTime properties (can be null)
    public DateTime? ExpiresAt { get; set; }
    public DateTime? RevokedAt { get; set; }
    public DateTime? LastUsedAt { get; set; }
}