using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Core.Interfaces;

public interface IBattleReportService
{
    /// <summary>
    /// Process a battle report request and return the battle data
    /// </summary>
    /// <param name="uploadId">Battle report by ID</param>
    /// <param name="battleDate">Date of the battle data</param>
    /// <param name="battleType">Type of the battle (Parking War or Others)</param>
    /// <param name="userId">Battle reports involving the Requester</param>
    /// <param name="participant">Battle reports involving the Player's name as a participant</param>
    /// <param name="inGameId">Battle reports involving the Player's in-game ID</param>
    /// <param name="groupTag">Battle report by group Tag</param>
    /// <param name="limit">How many reports should be retrieved</param>
    /// <param name="offset">Offset to the requested reports used for pagination</param>

    Task<(List<BattleReportResponse> Reports, int totalCount)> GetBattleReportAsync(
        Guid? uploadId = null,
        DateTime? battleDate = null,
        string? battleType = null,
        Guid? userId = null,
        string? participant = null,
        string? inGameId = null,
        string? groupTag = null,
        int limit = 10,
        int offset = 0);

    Task<BattleReportResponse?> GetBattleReportByIdAsync(Guid reportId);
}