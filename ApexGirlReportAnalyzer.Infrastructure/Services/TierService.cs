using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;
using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Infrastructure.Mappers;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;

namespace ApexGirlReportAnalyzer.Infrastructure.Services;

public class TierService : ITierService
{
    private readonly ILogger<TierService> _logger;
    private readonly AppDbContext _context;

    public TierService(
        ILogger<TierService> logger, 
        AppDbContext context)
    {
        _logger = logger;
        _context = context;
    }

    public async Task<List<TierResponse>> GetTiersAsync()
    {
        var tiers = await _context.Tiers
            .Include(t => t.TierLimits).ToListAsync();

        return tiers.Select(TierMapper.ToDto).ToList();
    }

    public async Task<TierResponse?> CreateTierAsync(CreateTierRequest request)
    {
        var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.Name == request.Name);
        
        if (tier != null)
            return null;

        tier = new Tier
        {
            Name = request.Name,
            IsDefault = request.IsDefault,
            TierLimits = new List<TierLimit>()
        };

        if (request.UserLimit != null)
        {
            tier.TierLimits.Add(new TierLimit
            {
                Scope = TierScope.User,
                DailyRequestLimit = request.UserLimit.DailyRequestLimit,
                MonthlyRequestLimit = request.UserLimit.MonthlyRequestLimit
            });
        }
            

        if (request.ServerLimit != null)
        {
            tier.TierLimits.Add(new TierLimit
            {
                Scope = TierScope.Server,
                DailyRequestLimit = request.ServerLimit.DailyRequestLimit,
                MonthlyRequestLimit = request.ServerLimit.MonthlyRequestLimit
            });
        }
            
        await _context.Tiers.AddAsync(tier);
        await _context.SaveChangesAsync();

        return TierMapper.ToDto(tier);
    }

    public async Task<TierResponse?> UpdateTierAsync(Guid tierId, UpdateTierRequest request)
    {
        var tier = await _context.Tiers
            .Include(t => t.TierLimits)
            .FirstOrDefaultAsync(t => t.Id == tierId);

        if (tier == null)
            return null;

        tier.Name = request.Name;
        tier.IsDefault = request.IsDefault;

        tier.TierLimits.Clear();

        if (request.ServerLimit != null) {
            tier.TierLimits.Add(new TierLimit
            {
                Scope = TierScope.Server,
                DailyRequestLimit = request.ServerLimit.DailyRequestLimit,
                MonthlyRequestLimit = request.ServerLimit.MonthlyRequestLimit
            });
        }

        if (request.UserLimit != null) {
            tier.TierLimits.Add(new TierLimit
            {
                Scope = TierScope.User,
                DailyRequestLimit = request.UserLimit.DailyRequestLimit,
                MonthlyRequestLimit = request.UserLimit.MonthlyRequestLimit
            });
        }

        await _context.SaveChangesAsync();

        return TierMapper.ToDto(tier);
    }

    public async Task<bool> DeleteTierAsync(Guid tierId)
    {

       if(await _context.Tiers.Where(t => t.Id == tierId).ExecuteDeleteAsync() == 0)
            return false;

        return true;
    }

    public async Task<bool> AssignTierToUserAsync(string discordUserId, Guid tierId)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.DiscordId == discordUserId);

        if (user == null)
            return false;

        var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.Id == tierId);
        if (tier == null)
            return false;

        user.Tier = tier;
        await _context.SaveChangesAsync();

        return true;
    }

    public async Task<bool> AssignTierToServerAsync(string discordServerId, Guid tierId)
    {
        var server = await _context.DiscordServers.FirstOrDefaultAsync(s => s.DiscordServerId == discordServerId);

        if (server == null)
            return false;

        var tier = await _context.Tiers.FirstOrDefaultAsync(t => t.Id == tierId);
        if (tier == null)
            return false;

        server.Tier = tier;
        await _context.SaveChangesAsync();

        return true;
    }
}
