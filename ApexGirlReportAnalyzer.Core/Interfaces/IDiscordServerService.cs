using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Core.Interfaces;

public interface IDiscordServerService
{
    Task<DiscordServerConfigResponse?> GetConfigAsync(String discordServerId);

    Task<DiscordServerConfigResponse> SetOrUpdateConfigAsync(DiscordServerConfigRequest configRequest);
}