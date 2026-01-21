using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Models.Entities
{
    public class BattleSide : BaseEntity
    {
        public BattleSideType Side { get; set; }
        public string Username { get; set; } = string.Empty;
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

        // Foreign Keys
        public Guid BattleReportId { get; set; }

        // Navigation properties
        public BattleReport BattleReport { get; set; } = null!;
    }
}
