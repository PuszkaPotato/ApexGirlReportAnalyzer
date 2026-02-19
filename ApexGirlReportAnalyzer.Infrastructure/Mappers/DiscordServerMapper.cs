using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Entities;


namespace ApexGirlReportAnalyzer.Infrastructure.Mappers;

/// <summary>
/// Maps UserResponse entities and their DTOs
/// </summary>
public static class DiscordServerMapper
{
    public static DiscordServerConfigResponse ToDto(DiscordServer server)
    {
        if (server == null)
        {
            throw new ArgumentNullException(nameof(server));
        }

        return new DiscordServerConfigResponse
        {
            DiscordServerId = server.DiscordServerId,
            UploadChannelId = server.UploadChannelId ?? string.Empty,
            AllowedRoleId = server.AllowedRoleId,
            LogChannelId = server.LogChannelId,
            OwnerDiscordId = server.OwnerDiscordId
        };
    }
}
