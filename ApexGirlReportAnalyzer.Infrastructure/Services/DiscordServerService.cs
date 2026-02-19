using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Migrations.Operations;

namespace ApexGirlReportAnalyzer.Infrastructure.Services
{
    public class DiscordServerService(AppDbContext context, ILogger<DiscordServerService> logger) : IDiscordServerService
    {
        private readonly AppDbContext _context = context;
        private readonly ILogger<DiscordServerService> _logger = logger;
        public async Task<Models.DTOs.DiscordServerConfigResponse?> GetConfigAsync(String discordServerId)
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
        public async Task<Models.DTOs.DiscordServerConfigResponse> SetOrUpdateConfigAsync(Models.DTOs.DiscordServerConfigRequest configRequest)
        {
            var server = await _context.DiscordServers
                .FirstOrDefaultAsync(s => s.DiscordServerId == configRequest.DiscordServerId);

            if (server != null)
            {
                server.UploadChannelId = configRequest.UploadChannelId;
                server.LogChannelId = configRequest.LogChannelId;
                server.OwnerDiscordId = configRequest.OwnerDiscordId;
                server.UpdatedAt = DateTime.UtcNow;
                _logger.LogInformation("Updated config for Discord Server ID: {DiscordServerId}", configRequest.DiscordServerId);
            }
            else
            {
                server = new Models.Entities.DiscordServer
                {
                    Id = Guid.NewGuid(),
                    DiscordServerId = configRequest.DiscordServerId,
                    UploadChannelId = configRequest.UploadChannelId,
                    LogChannelId = configRequest.LogChannelId,
                    OwnerDiscordId = configRequest.OwnerDiscordId,
                    DefaultReportPrivacy = Models.Enums.PrivacyScope.Public,
                    CreatedAt = DateTime.UtcNow,
                };
                await _context.DiscordServers.AddAsync(server);
                _logger.LogInformation("Created new config for Discord Server ID: {DiscordServerId}", configRequest.DiscordServerId);
            }

            await _context.SaveChangesAsync();

            return DiscordServerMapper.ToDto(server);
        }
    }
}
