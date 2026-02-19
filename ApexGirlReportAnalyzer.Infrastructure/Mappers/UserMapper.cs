using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Entities;


namespace ApexGirlReportAnalyzer.Infrastructure.Mappers;

/// <summary>
/// Maps UserResponse entities and their DTOs
/// </summary>
public static class UserMapper
{
    public static UserResponse ToDto(User user)
    {
        if (user == null)
        {
            throw new ArgumentNullException(nameof(user));
        }

        return new UserResponse
        {
            Id = user.Id,
            InGamePlayerId = user.InGamePlayerId,
            TierName = user.Tier?.Name ?? "Unknown",
            CreatedAt = user.CreatedAt,
        };
    }
}
