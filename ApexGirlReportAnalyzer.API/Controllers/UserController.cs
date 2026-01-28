using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController(IUserService userService, ILogger<UserController> logger) : ControllerBase
{
    private readonly IUserService _userService = userService;
    private readonly ILogger<UserController> _logger = logger;

    /// <summary>
    /// Check remaining upload quota for a user
    /// </summary>
    /// <param name="userId">User ID to check</param>
    [HttpGet("quota/{userId}")]
    [ProducesResponseType(typeof(QuotaInfo), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetQuota(Guid userId)
    {
        try
        {
            if (userId == Guid.Empty)
            {
                return BadRequest(new { error = "Valid user ID is required" });
            }

            var quota = await _userService.GetRemainingQuotaAsync(userId);
            return Ok(quota);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving quota for user {UserId}", userId);
            return NotFound(new { error = "User not found or quota could not be retrieved" });
        }
    }
}
