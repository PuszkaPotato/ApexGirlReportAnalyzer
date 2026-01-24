namespace ApexGirlReportAnalyzer.Models.DTOs;

/// <summary>
/// Represents one side (player or enemy) in a battle
/// </summary>
public class BattleSideResponse
{
    public string Username { get; set; } = string.Empty;
    public string? GroupTag { get; set; }
    public int? Level { get; set; }

    // Troop Statistics
    public int FanCount { get; set; }
    public int LossCount { get; set; }
    public int InjuredCount { get; set; }
    public int RemainingCount { get; set; }
    public int? ReinforceCount { get; set; }

    // Attributes
    public int Sing { get; set; }
    public int Dance { get; set; }

    // Skills (as percentages, not basis points - easier for OpenAI)
    public int ActiveSkill { get; set; }
    public int BasicAttackBonus { get; set; }
    public int ReduceBasicAttackDamage { get; set; }
    public int SkillBonus { get; set; }
    public int SkillReduction { get; set; }
    public int ExtraDamage { get; set; }
}