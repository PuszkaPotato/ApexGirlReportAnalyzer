using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class BattleReportController : ControllerBase
{
    private readonly ILogger<BattleReportController> _logger;
    private readonly IBattleReportService _battleReportService;

    public BattleReportController(ILogger<BattleReportController> logger, IBattleReportService battleReportService)
    {
        _logger = logger;
        _battleReportService = battleReportService;
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

    [HttpGet("reportId")]
    public async Task<IActionResult> GetBattleReportById([FromRoute] Guid reportId)
    {
        var report = await _battleReportService.GetBattleReportByIdAsync(reportId);
        if (report == null)
            return NotFound(new { Message = $"Battle report with ID {reportId} not found." });
        return Ok(report);
    }
}
