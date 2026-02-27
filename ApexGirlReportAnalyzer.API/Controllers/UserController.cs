using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UserController> _logger;

    public UserController(IUserService userService, ILogger<UserController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// Get or create a user
    /// </summary>
    /// <param name="discordId">User's Discord ID to be used to create or retrieve the user</param>
    [HttpPost("get-or-create")]
    [ProducesResponseType(typeof(UserResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> GetOrCreateUser([FromQuery]string discordId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(discordId))
            {
                return BadRequest(new { error = "Valid discord ID is required" });
            }
            var user = await _userService.GetOrCreateByDiscordIdAsync(discordId);
            return Ok(user);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving or creating user with ID {DiscordId}", discordId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while processing the request" });
        }
    }

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
