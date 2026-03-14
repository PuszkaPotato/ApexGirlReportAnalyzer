using ApexGirlReportAnalyzer.Bot.Http;

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

    public async Task<bool> AssignTierToUserAsync(string discordUserId, string tierName)
    {
        var tiers = await _apiClient.GetTiersAsync();

        if (tiers == null)
        {
            _logger.LogWarning("Failed to retrieve tiers when assigning tier {TierName} to user {DiscordUserId}", tierName, discordUserId);
            return false;
        }

        var tier = tiers.FirstOrDefault(t => t.Name.Equals(tierName, StringComparison.OrdinalIgnoreCase));

        if (tier == null)
        {
            _logger.LogWarning("Tier {TierName} not found", tierName);
            return false;
        }

        return await _apiClient.AssignTierToUserAsync(discordUserId, tier.Id);
    }
}
