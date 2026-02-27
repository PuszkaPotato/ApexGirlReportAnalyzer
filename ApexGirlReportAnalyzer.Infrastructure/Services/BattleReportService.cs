using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Infrastructure.Mappers;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApexGirlReportAnalyzer.Infrastructure.Services;

public class BattleReportService : IBattleReportService
{
    private readonly AppDbContext _context;
    private readonly ILogger<BattleReportService> _logger;

    public BattleReportService(
        AppDbContext context, 
        ILogger<BattleReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<(List<BattleReportResponse> Reports, int totalCount)> GetBattleReportAsync(
        Guid? uploadId = null,
        DateTime? battleDate = null, 
        string? battleType = null, 
        Guid? userId = null,
        string? participant = null,
        string? inGameId = null,  
        string? groupTag = null,
        int limit = 10,
        int offset = 0)
    {
        var query = _context.BattleReports
            .Include(br => br.BattleSides)
            .Include(br => br.Upload)
            .AsQueryable();

        if (uploadId.HasValue)
            query = query.Where(br => br.UploadId == uploadId.Value);
        if (battleDate.HasValue)
            query = query.Where(br => br.BattleDate.Date == battleDate.Value.Date);
        if (!string.IsNullOrEmpty(battleType))
            query = query.Where(br => br.BattleType == battleType);
        if (userId.HasValue)
            query = query.Where(br => br.Upload.UserId == userId.Value);
        if (!string.IsNullOrEmpty(participant))
            query = query.Where(br => br.BattleSides.Any(bs => bs.Username == participant));
        if (!string.IsNullOrEmpty(inGameId))
            query = query.Where(br => br.BattleSides.Any(bs => bs.InGamePlayerId == inGameId));
        if (!string.IsNullOrEmpty(groupTag))
            query = query.Where(br => br.BattleSides.Any(bs => bs.GroupTag == groupTag));

        var totalCount = await query.CountAsync();

        query = query
            .OrderByDescending(br => br.CreatedAt)
            .Skip(offset)
            .Take(limit);

        var battleReports = await query.ToListAsync();

        var mappedReports = battleReports.Select(br => BattleReportMapper.ToDto(br, br.Upload)).ToList();

        return (mappedReports, totalCount);
    }

    public async Task<BattleReportResponse?> GetBattleReportByIdAsync(Guid reportId)
    {
        var query = await _context.BattleReports
            .Include(br => br.BattleSides)
            .Include(br => br.Upload)
            .FirstOrDefaultAsync(br => br.Id == reportId);

        if (query == null)
            return null;

        return BattleReportMapper.ToDto(query, query.Upload);
    }

    public async Task<Guid> CreateBattleReportAsync(BattleReportResponse battleData, Guid uploadId, string? playerInGameId, string? enemyInGameId)
    {

        var battleReport = new BattleReport
        {
            Id = Guid.NewGuid(),
            UploadId = uploadId,
            BattleType = battleData.BattleType,
            BattleDate = battleData.BattleDate,
            CreatedAt = DateTime.UtcNow,
        };

        _context.BattleReports.Add(battleReport);

        var playerSide = BattleReportMapper.ToEntity(battleData.Player, battleReport.Id, BattleSideType.Player, playerInGameId);
        var enemySide = BattleReportMapper.ToEntity(battleData.Enemy, battleReport.Id, BattleSideType.Enemy, enemyInGameId);

        _context.BattleSides.Add(playerSide);
        _context.BattleSides.Add(enemySide);

        await _context.SaveChangesAsync();

        return battleReport.Id;

    }
}
