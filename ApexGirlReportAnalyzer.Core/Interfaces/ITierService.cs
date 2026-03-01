using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Core.Interfaces;

public interface ITierService
{

    /// <summary>
    /// Get all tiers with their limits
    /// </summary>
    Task<List<TierResponse>> GetTiersAsync();

    /// <summary>
    /// Creates a new tier using the specified request parameters.
    /// </summary>
    /// <param name="request">The request object containing the details, including Name, isDefault boolean, and limits for user and/or server.</param>
    Task<TierResponse?> CreateTierAsync(CreateTierRequest request);

    /// <summary>
    /// Updates an existing tier identified by the provided tierId using the specified request parameters.
    /// </summary>
    /// <param name="tierId">Guid based ID of the tier</param>
    /// <param name="request">The request object containing the details, including Name, isDefault boolean, and limits for user and/or server.</param>
    Task<TierResponse?> UpdateTierAsync(Guid tierId, UpdateTierRequest request);

    /// <summary>
    /// Delete the tier identified by the provided ID.
    /// </summary>
    /// <param name="tierId">Guid based ID of the tier</param>
    Task<DeleteTierResult> DeleteTierAsync(Guid tierId);

    /// <summary>
    /// Assigns a tier to a user.
    /// </summary>
    /// <param name="discordUserId">String based Discord ID of the user</param>
    /// <param name="tierId">Guid based ID of the tier</param>
    Task<bool> AssignTierToUserAsync(string discordUserId, Guid tierId);

    /// <summary>
    /// Assigns a tier to a server.
    /// </summary>
    /// <param name="discordServerId">String based Discord ID of the server</param>
    /// <param name="tierId">Guid based ID of the tier</param>
    Task<bool> AssignTierToServerAsync(string discordServerId, Guid tierId);

    /// <summary>
    /// Migrate users and servers assigned to the source tier to the target tier.
    /// </summary>
    /// <param name="sourceTierId">Guid based ID of the source tier</param>
    /// <param name="targetTierId">Guid based ID of the target tier</param>
    Task<bool> MigrateTierAssigneesAsync(Guid sourceTierId, Guid? targetTierId = null);
}