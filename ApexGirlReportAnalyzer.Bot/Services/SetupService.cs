using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Bot.Services;

public class SetupService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<SetupService> _logger;

    public SetupService(ApiClient apiClient, ILogger<SetupService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<DiscordServerConfigResponse?> SetServerConfigAsync(string serverId, string ownerId, string uploadChannelId, string? logChannelId, string? allowedRoleId, PrivacyScope privacyScope)
    {
        _logger.LogInformation("Setting up server configuration for server {ServerId}", serverId);
        var configRequest = new DiscordServerConfigRequest
        {
            DiscordServerId = serverId,
            OwnerDiscordId = ownerId,
            UploadChannelId = uploadChannelId,
            LogChannelId = logChannelId,
            AllowedRoleId = allowedRoleId,
            DefaultReportPrivacy = privacyScope
        };
        
        return await _apiClient.SetServerConfigAsync(configRequest);
    }
}
