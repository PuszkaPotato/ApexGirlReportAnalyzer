using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Models.Enums;


namespace ApexGirlReportAnalyzer.Infrastructure.Mappers;

/// <summary>
/// Maps between BattleReport/BattleSide entities and their DTOs
/// </summary>
public static class BattleReportMapper
{
    /// <summary>
    /// Maps a BattleSide entity to BattleSideDto
    /// </summary>
    public static BattleSideDto ToDto(BattleSide? side)
    {
        if (side == null)
        {
            return new BattleSideDto();
        }

        return new BattleSideDto
        {
            Username = side.Username,
            InGamePlayerId = side.InGamePlayerId,
            GroupTag = side.GroupTag,
            Level = side.Level,
            FanCount = side.FanCount,
            LossCount = side.LossCount,
            InjuredCount = side.InjuredCount,
            RemainingCount = side.RemainingCount,
            ReinforceCount = side.ReinforceCount,
            Sing = side.Sing,
            Dance = side.Dance,
            ActiveSkill = side.ActiveSkill,
            BasicAttackBonus = side.BasicAttackBonus,
            ReduceBasicAttackDamage = side.ReduceBasicAttackDamage,
            SkillBonus = side.SkillBonus,
            SkillReduction = side.SkillReduction,
            ExtraDamage = side.ExtraDamage
        };
    }

    /// <summary>
    /// Maps a BattleReport entity (with BattleSides loaded) to BattleReportResponse
    /// </summary>
    public static BattleReportResponse ToDto(BattleReport report, Upload? upload = null)
    {
        var playerSide = report.BattleSides?.FirstOrDefault(bs => bs.Side == BattleSideType.Player);
        var enemySide = report.BattleSides?.FirstOrDefault(bs => bs.Side == BattleSideType.Enemy);

        return new BattleReportResponse
        {
            ReportId = report.Id,
            BattleType = report.BattleType,
            BattleDate = report.BattleDate,
            UploadedAt = upload?.CreatedAt,
            Player = ToDto(playerSide),
            Enemy = ToDto(enemySide),
            TokensUsed = upload?.TokenEstimate,
            EstimatedCost = upload?.EstimatedCostEuro,
            PromptVersion = upload?.PromptVersion ?? string.Empty
        };
    }

    /// <summary>
    /// Creates a BattleSide entity from a DTO
    /// </summary>
    public static BattleSide ToEntity(
        BattleSideDto dto,
        Guid battleReportId,
        BattleSideType sideType,
        string? manualInGameId = null)
    {
        return new BattleSide
        {
            Id = Guid.NewGuid(),
            BattleReportId = battleReportId,
            Side = sideType,
            Username = dto.Username ?? string.Empty,
            InGamePlayerId = manualInGameId ?? dto.InGamePlayerId,
            GroupTag = dto.GroupTag,
            Level = dto.Level,
            FanCount = dto.FanCount,
            LossCount = dto.LossCount,
            InjuredCount = dto.InjuredCount,
            RemainingCount = dto.RemainingCount,
            ReinforceCount = dto.ReinforceCount,
            Sing = dto.Sing,
            Dance = dto.Dance,
            ActiveSkill = dto.ActiveSkill,
            BasicAttackBonus = dto.BasicAttackBonus,
            ReduceBasicAttackDamage = dto.ReduceBasicAttackDamage,
            SkillBonus = dto.SkillBonus,
            SkillReduction = dto.SkillReduction,
            ExtraDamage = dto.ExtraDamage,
            CreatedAt = DateTime.UtcNow
        };
    }
}
