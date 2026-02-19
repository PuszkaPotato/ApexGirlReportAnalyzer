namespace ApexGirlReportAnalyzer.Models.DTOs;

public class DiscordServerConfigResponse
{
    /// <summary>
    /// Unique identifier for the Discord server (guild)
    /// </summary>
    public string DiscordServerId { get; set; } = null!;
    /// <summary>
    /// ChannelUploadId is the ID of the channel where users can upload their battle reports. The bot will monitor this channel for new uploads.
    /// </summary>
    public string UploadChannelId { get; set; } = null!;
    /// <summary>
    /// AllowedRoleId is the ID of the role that can use server quota on the Discord Server. If not set then all server members with access to the upload channel can use the server quota. 
    /// Setting this allows server owners to restrict upload permissions to a specific role, ensuring that only authorized users can consume the server's upload quota.
    /// </summary>
    public string? AllowedRoleId { get; set; }
    /// <summary>
    /// ModeratorChannelId is the ID of the channel where the bot will send notifications about new uploads, processing status, and any issues that arise. 
    /// This allows moderators to stay informed on who is using their server quota and the status of their uploads.
    /// </summary>
    public string? LogChannelId { get; set; }
    /// <summary>
    /// ServerOwnerId is the Discord ID of the server owner. This is used to ensure that only the owner can update the server configuration and manage the server's quota.
    /// </summary>
    public string OwnerDiscordId { get; set; } = null!;
}
