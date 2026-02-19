namespace ApexGirlReportAnalyzer.Models.Entities;

public class Tier : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; } = false; // Indicates if this is the default tier assigned to new users

    // Navigation properties (relationships)
    public ICollection<TierLimit> TierLimits { get; set; } = new List<TierLimit>();
    public ICollection<User> Users { get; set; } = new List<User>();
    public ICollection<DiscordServer> DiscordServers { get; set; } = new List<DiscordServer>();
}