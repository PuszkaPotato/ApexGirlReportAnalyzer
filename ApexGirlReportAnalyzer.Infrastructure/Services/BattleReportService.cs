using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Infrastructure.Mappers;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Models.Enums;
using Microsoft.EntityFrameworkCore;
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

    public async Task<string> ExportBattleReportsCsvAsync(
        string? requestingDiscordUserId = null,
        bool isDeveloper = false,
        string? participant = null,
        string? battleType = null,
        DateTime? battleDate = null,
        string? groupTag = null)
    {
        var query = _context.BattleReports
            .Include(br => br.BattleSides)
            .Include(br => br.Upload)
                .ThenInclude(u => u.User)
            .AsQueryable();

        if (!isDeveloper)
        {
            query = query.Where(br =>
                br.Upload.PrivacyScope == PrivacyScope.Public ||
                (requestingDiscordUserId != null && br.Upload.User.DiscordId == requestingDiscordUserId));
        }

        if (!string.IsNullOrEmpty(participant))
            query = query.Where(br => br.BattleSides.Any(bs => bs.Username == participant));
        if (!string.IsNullOrEmpty(battleType))
            query = query.Where(br => br.BattleType == battleType);
        if (battleDate.HasValue)
            query = query.Where(br => br.BattleDate.Date == battleDate.Value.Date);
        if (!string.IsNullOrEmpty(groupTag))
            query = query.Where(br => br.BattleSides.Any(bs => bs.GroupTag == groupTag));

        var reports = await query.OrderByDescending(br => br.BattleDate).ToListAsync();

        var csv = new System.Text.StringBuilder();
        csv.AppendLine(
            "ReportId,BattleDate,BattleType,UploadedAt," +
            "Player_Username,Player_InGameId,Player_GroupTag,Player_Level,Player_TeamRank,Player_Server,Player_Fans,Player_Losses,Player_Injured,Player_Remaining,Player_Sing,Player_Dance,Player_ActiveSkill,Player_BasicAttack,Player_SkillBonus,Player_SkillReduction,Player_ExtraDamage," +
            "Enemy_Username,Enemy_InGameId,Enemy_GroupTag,Enemy_Level,Enemy_TeamRank,Enemy_Server,Enemy_Fans,Enemy_Losses,Enemy_Injured,Enemy_Remaining,Enemy_Sing,Enemy_Dance,Enemy_ActiveSkill,Enemy_BasicAttack,Enemy_SkillBonus,Enemy_SkillReduction,Enemy_ExtraDamage");

        foreach (var report in reports)
        {
            var mapped = BattleReportMapper.ToDto(report, report.Upload);
            var p = mapped.Player;
            var e = mapped.Enemy;

            csv.AppendLine(string.Join(",",
                report.Id,
                report.BattleDate.ToString("yyyy-MM-dd"),
                CsvEscape(report.BattleType),
                report.Upload?.CreatedAt.ToString("yyyy-MM-ddTHH:mm:ss") ?? "",
                CsvEscape(p.Username), CsvEscape(p.InGamePlayerId), CsvEscape(p.GroupTag),
                p.Level, p.TeamRank, p.Server, p.FanCount, p.LossCount, p.InjuredCount, p.RemainingCount ?? 0,
                p.Sing, p.Dance, p.ActiveSkill, p.BasicAttackBonus, p.SkillBonus, p.SkillReduction, p.ExtraDamage,
                CsvEscape(e.Username), CsvEscape(e.InGamePlayerId), CsvEscape(e.GroupTag),
                e.Level, e.TeamRank, e.Server, e.FanCount, e.LossCount, e.InjuredCount, e.RemainingCount ?? 0,
                e.Sing, e.Dance, e.ActiveSkill, e.BasicAttackBonus, e.SkillBonus, e.SkillReduction, e.ExtraDamage));
        }

        return csv.ToString();
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value)) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    public async Task<Guid> CreateBattleReportAsync(BattleReportResponse battleData, Guid uploadId, string? playerInGameId, string? enemyInGameId, int? playerTeamRank = null, int? enemyTeamRank = null, int? playerServer = null, int? enemyServer = null)
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

        var playerSide = BattleReportMapper.ToEntity(battleData.Player, battleReport.Id, BattleSideType.Player, playerInGameId, playerTeamRank, playerServer);
        var enemySide = BattleReportMapper.ToEntity(battleData.Enemy, battleReport.Id, BattleSideType.Enemy, enemyInGameId, enemyTeamRank, enemyServer);

        _context.BattleSides.Add(playerSide);
        _context.BattleSides.Add(enemySide);

        await _context.SaveChangesAsync();

        return battleReport.Id;

    }
}
