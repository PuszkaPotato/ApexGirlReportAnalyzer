using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ApexGirlReportAnalyzer.API.Controllers;

/// <summary>
/// Handles retrieval and querying of battle reports.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BattleReportController : ControllerBase
{
    private readonly ILogger<BattleReportController> _logger;
    private readonly IBattleReportService _battleReportService;
    private readonly IConfiguration _configuration;

    /// <inheritdoc />
    public BattleReportController(ILogger<BattleReportController> logger, IBattleReportService battleReportService, IConfiguration configuration)
    {
        _logger = logger;
        _battleReportService = battleReportService;
        _configuration = configuration;
    }

    /// <summary>
    /// Request battle reports
    /// By default returns last 10 reports according to applied filters, if no filters then returns last 10 reports uploaded by the user
    /// </summary>
    /// <param name="uploadId">Battle report by ID</param>
    /// <param name="battleDate">Date of the battle data</param>
    /// <param name="battleType">Type of the battle (Parking War or Others)</param>
    /// <param name="userId">Battle reports involving the Requester</param>
    /// <param name="participant">Battle reports involving the Player's name as a participant</param>
    /// <param name="inGameId">Battle reports involving the Player's in-game ID</param>
    /// <param name="groupTag">Battle report by group Tag</param>
    /// <param name="limit">Maximum number of reports to return (default 10)</param>
    /// <param name="offset">Number of reports to skip for pagination (default 0)</param>
    [HttpGet]
    public async Task<IActionResult> GetBattleReports(
        [FromQuery] Guid? uploadId = null,
        [FromQuery] DateTime? battleDate = null,
        [FromQuery] string? battleType = null,
        [FromQuery] Guid? userId = null,
        [FromQuery] string? participant = null,
        [FromQuery] string? inGameId = null,
        [FromQuery] string? groupTag = null,
        [FromQuery] int limit = 10,
        [FromQuery] int offset = 0)
    {
        var (reports, totalCount) = await _battleReportService.GetBattleReportAsync(uploadId, battleDate, battleType, userId, participant, inGameId, groupTag, limit, offset);

        var response = new BattleReportListResponse
        {
            FiltersApplied = new BattleReportFilterInfo
            {
                UploadId = uploadId,
                BattleDate = battleDate,
                BattleType = battleType,
                UserId = userId,
                Participant = participant,
                InGameId = inGameId,
                GroupTag = groupTag
            },
            TotalCount = totalCount,
            Count = reports.Count,
            BattleReports = reports
        };
        return Ok(response);
    }

    /// <summary>
    /// Export battle reports as a CSV file, filtered by privacy scope.
    /// </summary>
    [HttpGet("export")]
    public async Task<IActionResult> ExportBattleReportsCsv(
        [FromQuery] string? requestingDiscordUserId = null,
        [FromQuery] string? participant = null,
        [FromQuery] string? battleType = null,
        [FromQuery] DateTime? battleDate = null,
        [FromQuery] string? groupTag = null)
    {
        var developerDiscordId = _configuration["App:DeveloperDiscordId"];
        var isDeveloper = !string.IsNullOrEmpty(developerDiscordId) &&
                          requestingDiscordUserId == developerDiscordId;

        var csv = await _battleReportService.ExportBattleReportsCsvAsync(
            requestingDiscordUserId, isDeveloper, participant, battleType, battleDate, groupTag);

        var fileName = $"battle-reports-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        var bytes = System.Text.Encoding.UTF8.GetBytes(csv);
        return File(bytes, "text/csv", fileName);
    }

    /// <summary>
    /// Get a single battle report by its ID.
    /// </summary>
    /// <param name="reportId">The ID of the battle report to retrieve.</param>
    [HttpGet("reportId")]
    public async Task<IActionResult> GetBattleReportById([FromQuery] Guid reportId)
    {
        var report = await _battleReportService.GetBattleReportByIdAsync(reportId);
        if (report == null)
            return NotFound(new ErrorResponse { Message = $"No battle report found with ID: {reportId}", Type = "NotFound" });
        return Ok(report);
    }
}
