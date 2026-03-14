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
        int limit = 5,
        int offset = 0)
    {
        _logger.LogInformation("Fetching battle reports — participant: {Participant}, limit: {Limit}", participant, limit);
        return await _apiClient.GetBattleReportsAsync(userId, participant, limit, offset);
    }
}
