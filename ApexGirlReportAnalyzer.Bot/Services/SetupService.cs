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

    public async Task<DiscordServerConfigResponse?> GetServerConfigAsync(string serverId)
    {
        _logger.LogInformation("Getting server configuration for server {ServerId}", serverId);
        return await _apiClient.GetServerConfigAsync(serverId);
    }

    public async Task<DiscordServerConfigResponse?> UpdateServerConfigAsync(
        string serverId,
        string ownerId,
        string? uploadChannelId,
        string? logChannelId,
        string? allowedRoleId,
        PrivacyScope? privacyScope)
    {
        _logger.LogInformation("Updating server configuration for server {ServerId}", serverId);

        var current = await _apiClient.GetServerConfigAsync(serverId);
        if (current == null)
        {
            _logger.LogWarning("Cannot update config for server {ServerId} — not yet configured", serverId);
            return null;
        }

        var request = new DiscordServerConfigRequest
        {
            DiscordServerId = serverId,
            OwnerDiscordId = ownerId,
            UploadChannelId = uploadChannelId ?? current.UploadChannelId,
            LogChannelId = logChannelId ?? current.LogChannelId,
            AllowedRoleId = allowedRoleId ?? current.AllowedRoleId,
            DefaultReportPrivacy = privacyScope ?? current.DefaultReportPrivacy
        };

        return await _apiClient.SetServerConfigAsync(request);
    }
}
