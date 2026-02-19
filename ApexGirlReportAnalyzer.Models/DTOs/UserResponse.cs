namespace ApexGirlReportAnalyzer.Models.DTOs;

public class UserResponse
{
    /// <summary>
    /// Gets or sets the unique identifier for the entity.
    /// </summary>
    public Guid Id { get; set; }
    /// <summary>
    /// Gets or sets the In Game Apex Girl Player Id. This is the unique identifier for the player in the Apex.
    /// </summary>
    public string? InGamePlayerId { get; set; }
    /// <summary>
    /// Name of the tier assigned to this user. This is included in the response for convenience, so that clients can easily display the user's tier without needing to make an additional request to fetch the tier details.
    /// </summary>
    public string? TierName { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the entity was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

}
