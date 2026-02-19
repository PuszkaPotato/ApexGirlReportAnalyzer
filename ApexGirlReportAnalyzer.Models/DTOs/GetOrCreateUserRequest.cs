namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Request model for user retrieval or creation. This can be used when a user logs in for the first time or when we need to fetch their details.
/// </summary>
public class GetOrCreateUserRequest
{
    /// <summary>
    /// DiscordId doesn't follow Guid structure, hence we use string type. 
    /// It represents the unique identifier for a user on Discord, which is typically a long numeric string. 
    /// This ID is used to link the user's Discord account with their profile in our system, 
    /// allowing us to associate their uploads and interactions with their Discord identity.
    /// </summary>
    public string DiscordId { get; set; } = null!;
}
