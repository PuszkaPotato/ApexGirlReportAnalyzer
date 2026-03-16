namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Request model for screenshot upload
/// Note: IFormFile will be handled separately in the controller
/// This DTO contains the metadata for the upload
/// </summary>
public class UploadRequest
{
    /// <summary>
    /// User ID making the upload (required)
    /// For now, manually provided - Discord bot will provide this automatically
    /// </summary>
    public Guid UserId { get; set; }

    /// <summary>
    /// Optional: Discord server ID if uploaded via Discord
    /// </summary>
    public string? DiscordServerId { get; set; }

    /// <summary>
    /// Optional: Original filename for tracking purposes
    /// </summary>
    public string? OriginalFileName { get; set; }

    /// <summary>
    /// Optional: Player's in-game ID, provided manually by the user
    /// </summary>
    public string? PlayerInGameId { get; set; }

    /// <summary>
    /// Optional: Enemy's in-game ID, provided manually by the user
    /// </summary>
    public string? EnemyInGameId { get; set; }

    /// <summary>
    /// Optional: Player's team rank (1-6), provided manually by the user
    /// </summary>
    public int? PlayerTeamRank { get; set; }

    /// <summary>
    /// Optional: Enemy's team rank (1-6), provided manually by the user
    /// </summary>
    public int? EnemyTeamRank { get; set; }

    /// <summary>
    /// Optional: Player's server number, provided manually by the user
    /// </summary>
    public int? PlayerServer { get; set; }

    /// <summary>
    /// Optional: Enemy's server number, provided manually by the user
    /// </summary>
    public int? EnemyServer { get; set; }
}