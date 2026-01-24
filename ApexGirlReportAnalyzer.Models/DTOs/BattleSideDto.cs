namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// DTO representing one side of a battle (player or enemy)
/// </summary>
public class BattleSideDto
{
    public string? Username { get; set; }
    public string? InGamePlayerId { get; set; }
    public string? GroupTag { get; set; }
    public int? Level { get; set; }

    // Troop Statistics
    public int FanCount { get; set; }
    public int LossCount { get; set; }
    public int InjuredCount { get; set; }
    public int? RemainingCount { get; set; }
    public int? ReinforceCount { get; set; }

    // Attributes
    public int Sing { get; set; }
    public int Dance { get; set; }

    // Skills
    public int ActiveSkill { get; set; }
    public int BasicAttackBonus { get; set; }
    public int ReduceBasicAttackDamage { get; set; }
    public int SkillBonus { get; set; }
    public int SkillReduction { get; set; }
    public int ExtraDamage { get; set; }
}