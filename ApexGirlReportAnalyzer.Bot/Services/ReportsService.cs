using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Bot.Services;

public class ReportsService
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<ReportsService> _logger;

    public ReportsService(ApiClient apiClient, ILogger<ReportsService> logger)
    {
        _apiClient = apiClient;
        _logger = logger;
    }

    public async Task<BattleReportListResponse?> GetReportsAsync(
        Guid? userId = null,
        string? participant = null,
        string? battleType = null,
        string? groupTag = null,
        string? inGameId = null,
        DateTime? battleDate = null,
        int limit = 10,
        int offset = 0)
    {
        _logger.LogInformation("Fetching battle reports — participant: {Participant}, limit: {Limit}", participant, limit);
        return await _apiClient.GetBattleReportsAsync(userId, participant, battleType, groupTag, inGameId, battleDate, limit, offset);
    }

    public async Task<BattleReportResponse?> GetReportByIdAsync(Guid reportId)
    {
        _logger.LogInformation("Fetching battle report {ReportId}", reportId);
        return await _apiClient.GetBattleReportByIdAsync(reportId);
    }

    public async Task<Stream?> ExportReportsCsvAsync(
        string? requestingDiscordUserId = null,
        string? participant = null,
        string? battleType = null,
        string? groupTag = null)
    {
        _logger.LogInformation("Exporting battle reports for user {UserId}", requestingDiscordUserId);
        return await _apiClient.ExportBattleReportsCsvAsync(requestingDiscordUserId, participant, battleType, groupTag: groupTag);
    }
}
