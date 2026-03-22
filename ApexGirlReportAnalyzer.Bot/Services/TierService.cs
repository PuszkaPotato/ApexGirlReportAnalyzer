using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Bot.Services;

public class TierService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<TierService> _logger;

    public TierService(ApiClient apiClient, ILogger<TierService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<List<TierResponse>?> GetTiersAsync()
    {
        return await _apiClient.GetTiersAsync();
    }

    public async Task<(bool success, bool userNotRegistered)> AssignTierToUserAsync(string discordUserId, string tierName)
    {
        var tier = await ResolveTierByNameAsync(tierName);
        if (tier == null) return (false, false);

        return await _apiClient.AssignTierToUserAsync(discordUserId, tier.Id);
    }

    public async Task<bool> AssignTierToServerAsync(string discordServerId, string tierName)
    {
        var tier = await ResolveTierByNameAsync(tierName);
        if (tier == null) return false;

        return await _apiClient.AssignTierToServerAsync(discordServerId, tier.Id);
    }

    public async Task<QuotaInfo?> GetUserQuotaAsync(string discordUserId)
    {
        var user = await _apiClient.GetOrCreateUserAsync(discordUserId);
        if (user == null)
        {
            _logger.LogWarning("Failed to resolve user for Discord ID {DiscordUserId}", discordUserId);
            return null;
        }

        return await _apiClient.GetUserQuotaAsync(user.Id);
    }

    private async Task<TierResponse?> ResolveTierByNameAsync(string tierName)
    {
        var tiers = await _apiClient.GetTiersAsync();

        if (tiers == null)
        {
            _logger.LogWarning("Failed to retrieve tiers when resolving tier {TierName}", tierName);
            return null;
        }

        var tier = tiers.FirstOrDefault(t => t.Name.Equals(tierName, StringComparison.OrdinalIgnoreCase));

        if (tier == null)
            _logger.LogWarning("Tier {TierName} not found", tierName);

        return tier;
    }
}
