using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Infrastructure.Mappers;

public static class TierMapper
{
    public static TierResponse ToDto(Tier tier)
    {
        if (tier == null)
        {
            throw new ArgumentNullException(nameof(tier));
        }
        return new TierResponse
        {
            Id = tier.Id,
            Name = tier.Name,
            Limits = tier.TierLimits.Select(l => new TierLimitResponse
            {
                Scope = l.Scope.ToString(),
                DailyRequestLimit = l.DailyRequestLimit,
                MonthlyRequestLimit = l.MonthlyRequestLimit
            }).ToList()
        };
    }
}
