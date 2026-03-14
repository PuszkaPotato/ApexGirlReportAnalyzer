using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Mappers;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ApexGirlReportAnalyzer.Infrastructure.Services;
public class DiscordServerService : IDiscordServerService
{
    private readonly AppDbContext _context;
    private readonly ILogger<DiscordServerService> _logger;

    public DiscordServerService(AppDbContext context, ILogger<DiscordServerService> logger) 
    {
        _logger = logger;
        _context = context;
    }
    public async Task<DiscordServerConfigResponse?> GetConfigAsync(string discordServerId)
    {
        var server = await _context.DiscordServers
            .FirstOrDefaultAsync(s => s.DiscordServerId == discordServerId);

        if (server != null)
        {
            _logger.LogInformation("Config found for Discord Server ID: {DiscordServerId}", discordServerId);
            return DiscordServerMapper.ToDto(server);
        }

        _logger.LogWarning("No config found for Discord Server ID: {DiscordServerId}", discordServerId);
        return null;
    }
    public async Task<DiscordServerConfigResponse> SetOrUpdateConfigAsync(DiscordServerConfigRequest configRequest)
    {
        var server = await _context.DiscordServers
            .FirstOrDefaultAsync(s => s.DiscordServerId == configRequest.DiscordServerId);

        if (server != null)
        {
            server.UploadChannelId = configRequest.UploadChannelId;
            server.LogChannelId = configRequest.LogChannelId;
            server.OwnerDiscordId = configRequest.OwnerDiscordId;
            server.DefaultReportPrivacy = configRequest.DefaultReportPrivacy;
            server.UpdatedAt = DateTime.UtcNow;
            _logger.LogInformation("Updated config for Discord Server ID: {DiscordServerId}", configRequest.DiscordServerId);
        }
        else
        {
            server = new DiscordServer
            {
                Id = Guid.NewGuid(),
                DiscordServerId = configRequest.DiscordServerId,
                UploadChannelId = configRequest.UploadChannelId,
                LogChannelId = configRequest.LogChannelId,
                OwnerDiscordId = configRequest.OwnerDiscordId,
                DefaultReportPrivacy = configRequest.DefaultReportPrivacy,
                CreatedAt = DateTime.UtcNow,
            };
            await _context.DiscordServers.AddAsync(server);
            _logger.LogInformation("Created new config for Discord Server ID: {DiscordServerId}", configRequest.DiscordServerId);
        }

        await _context.SaveChangesAsync();

        return DiscordServerMapper.ToDto(server);
    }
}
