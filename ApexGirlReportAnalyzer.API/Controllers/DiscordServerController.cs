using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers;

/// <summary>
/// Handles Discord server configuration management.
/// </summary>
[Authorize]
[ApiController]
[Route("api/[controller]")]
public class DiscordServerController : ControllerBase
{
    private readonly ILogger<DiscordServerController> _logger;
    private readonly IDiscordServerService _discordServerService;

    /// <inheritdoc />
    public DiscordServerController(IDiscordServerService discordServerService, ILogger<DiscordServerController> logger)
    {
        _logger = logger;
        _discordServerService = discordServerService;
    }

    /// <summary>
    /// Returns the configuration of a requested Discord server, including upload channel, allowed role, log channel, and owner Discord ID.
    /// </summary>
    /// <param name="discordServerId"></param>
    [HttpGet("{discordServerId}")]
    [ProducesResponseType(typeof(DiscordServerConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetConfig(string discordServerId)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(discordServerId))
            {
                return BadRequest(new { error = "Valid Discord Server ID is required" });
            }
            var config = await _discordServerService.GetConfigAsync(discordServerId);
            if (config == null)
            {
                return NotFound(new { error = "No config found for the provided Discord Server ID" });
            }
            return Ok(config);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving config for Discord Server ID: {DiscordServerId}", discordServerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while retrieving the config" });
        }

    }

    /// <summary>
    /// Sets or updates the configuration for a Discord server.
    /// </summary>
    /// <param name="configRequest"></param>
    [HttpPost("config")]
    [ProducesResponseType(typeof(DiscordServerConfigResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> SetOrUpdateConfig([FromBody] DiscordServerConfigRequest configRequest)
    {
        try
        {
            if (configRequest == null || string.IsNullOrWhiteSpace(configRequest.DiscordServerId))
            {
                return BadRequest(new { error = "Valid config request with Discord Server ID is required" });
            }
            var updatedConfig = await _discordServerService.SetOrUpdateConfigAsync(configRequest);
            return Ok(updatedConfig);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error setting/updating config for Discord Server ID: {DiscordServerId}", configRequest?.DiscordServerId);
            return StatusCode(StatusCodes.Status500InternalServerError, new { error = "An error occurred while setting/updating the config" });
        }
    }
}