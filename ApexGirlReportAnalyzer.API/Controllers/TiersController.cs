using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers;

/// <summary>
/// Handles tier management including creation, updates, deletion, and assignment to users and servers.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class TiersController : ControllerBase
{
    private readonly ILogger<TiersController> _logger;
    private readonly ITierService _tierService;

    /// <inheritdoc />
    public TiersController(
        ILogger<TiersController> logger,
        ITierService tierService)
    {
        _logger = logger;
        _tierService = tierService;
    }

    /// <summary>
    /// Get all the tiers and their information.
    /// </summary>
    [HttpGet]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetTiers()
    {
        try
        {
            var tiers = await _tierService.GetTiersAsync();

            return Ok(tiers);

        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while retrieving tiers.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while processing your request.", Type = "InternalServerError" });
        }
    }

    /// <summary>
    /// Create a new tier with the specified details.
    /// </summary>
    /// 
    [HttpPost]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> CreateTier([FromBody] CreateTierRequest request)
    {
        try
        {
            var createdTier = await _tierService.CreateTierAsync(request);
            if (createdTier == null)
            {
                return BadRequest(new ErrorResponse { Message = "A tier with that name already exists.", Type = "Conflict" });
            }
            return Ok(createdTier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while creating a new tier.");
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while processing your request.", Type = "InternalServerError" });
        }
    }

    /// <summary>
    /// Update an existing tier with the specified details.
    /// </summary>
    /// 
    [HttpPut("{tierId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> UpdateTier(Guid tierId, [FromBody] UpdateTierRequest request)
    {
        try
        {
            var updatedTier = await _tierService.UpdateTierAsync(tierId, request);
            if (updatedTier == null)
            {
                return NotFound(new ErrorResponse { Message = "Tier not found.", Type = "NotFound" });
            }
            return Ok(updatedTier);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while updating the tier with ID {TierId}.", tierId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while processing your request.", Type = "InternalServerError" });
        }
    }

    /// <summary>
    /// Assign a tier to a user.
    /// </summary>
    /// 
    [HttpPut("{tierId}/assign-user/{discordUserId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignTierToUser([FromRoute] string discordUserId, [FromRoute] Guid tierId)
    {
        try
        {
            var result = await _tierService.AssignTierToUserAsync(discordUserId, tierId);
            if (!result)
            {
                return NotFound(new ErrorResponse { Message = "User or tier not found.", Type = "NotFound" });
            }
            return Ok("Tier assigned to user successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while assigning tier with ID {TierId} to user with Discord ID {DiscordUserId}.", tierId, discordUserId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while processing your request.", Type = "InternalServerError" });
        }
    }

    /// <summary>
    /// Assign a tier to a server.
    /// </summary>
    [HttpPut("{tierId}/assign-server/{discordServerId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> AssignTierToServer([FromRoute] string discordServerId, [FromRoute] Guid tierId)
    {
        try
        {
            var result = await _tierService.AssignTierToServerAsync(discordServerId, tierId);
            if (!result)
            {
                return NotFound(new ErrorResponse { Message = "Server or tier not found.", Type = "NotFound" });
            }
            return Ok("Tier assigned to server successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while assigning tier with ID {TierId} to server with Discord ID {DiscordServerId}.", tierId, discordServerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while processing your request.", Type = "InternalServerError" });
        }
    }

    /// <summary>
    /// Delete a tier by its ID.
    /// </summary>
    [HttpDelete("{tierId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> DeleteTier(Guid tierId)
    {
        try
        {
            var result = await _tierService.DeleteTierAsync(tierId);
            if (result == DeleteTierResult.NotFound)
            {
                return NotFound(new ErrorResponse { Message = "Tier not found.", Type = "NotFound" });
            } else if (result == DeleteTierResult.InUse)
            {
                return Conflict(new ErrorResponse { Message = "The tier is currently in use and cannot be deleted.", Type = "Conflict" });
            }
            return Ok("Tier deleted successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while deleting the tier with ID {TierId}.", tierId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while processing your request.", Type = "InternalServerError" });
        }
    }

    /// <summary>
    /// Migrate all assignees (users and servers) from the source tier to the target tier. If targetTierId is null, the assignees will be assigned to a default tier.
    /// </summary>
    [HttpPut("migrate-assignees/{sourceTierId}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> MigrateTierAssignees(Guid sourceTierId, [FromQuery] Guid? targetTierId = null)
    {
        try
        {
            var result = await _tierService.MigrateTierAssigneesAsync(sourceTierId, targetTierId);
            if (!result)
            {
                return NotFound(new ErrorResponse { Message = "Source tier not found.", Type = "NotFound" });
            }
            return Ok("Tier assignees migrated successfully.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred while migrating assignees from tier with ID {SourceTierId} to tier with ID {TargetTierId}.", sourceTierId, targetTierId);
            return StatusCode(StatusCodes.Status500InternalServerError, new ErrorResponse { Message = "An error occurred while processing your request.", Type = "InternalServerError" });
        }
    }
}