using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UploadController(IUploadService uploadService, ILogger<UploadController> logger) : ControllerBase
{
    private readonly IUploadService _uploadService = uploadService;
    private readonly ILogger<UploadController> _logger = logger;

    /// <summary>
    /// Upload a battle report screenshot for analysis
    /// </summary>
    /// <param name="image">Screenshot image file (PNG or JPEG)</param>
    /// <param name="userId">User ID making the upload</param>
    /// <param name="discordServerId">Optional: Discord server ID if uploaded via bot</param>
    /// <param name="playerInGameId">Optional: Player's in-game ID</param>
    /// <param name="enemyInGameId">Optional: Enemy's in-game ID</param>
    /// <returns>Analysis results or error</returns>
    [HttpPost]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(UploadResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> Upload(
        IFormFile image,
        [FromForm] Guid userId,
        [FromForm] string? discordServerId = null,
        [FromForm] string? playerInGameId = null,
        [FromForm] string? enemyInGameId = null)
    {
        try
        {
            // Validate image presence
            if (image == null || image.Length == 0)
            {
                return BadRequest(new UploadResponse
                {
                    Success = false,
                    ErrorMessage = "No image provided"
                });
            }

            // Validate user ID
            if (userId == Guid.Empty)
            {
                return BadRequest(new UploadResponse
                {
                    Success = false,
                    ErrorMessage = "Valid user ID is required"
                });
            }

            // Validate file type
            var allowedTypes = new[] { "image/png", "image/jpeg", "image/jpg" };
            if (!allowedTypes.Contains(image.ContentType.ToLower()))
            {
                return BadRequest(new UploadResponse
                {
                    Success = false,
                    ErrorMessage = "Only PNG and JPEG images are allowed"
                });
            }

            // Validate file size (max 10MB)
            const int maxSizeBytes = 10 * 1024 * 1024;
            if (image.Length > maxSizeBytes)
            {
                return BadRequest(new UploadResponse
                {
                    Success = false,
                    ErrorMessage = "Image too large (max 10MB)"
                });
            }

            _logger.LogInformation(
                "Processing upload for user {UserId}. File: {FileName}, Size: {Size} bytes",
                userId, image.FileName, image.Length);

            // Convert to base64
            using var memoryStream = new MemoryStream();
            await image.CopyToAsync(memoryStream);
            var imageBytes = memoryStream.ToArray();
            var base64Image = Convert.ToBase64String(imageBytes);

            // Process upload
            var result = await _uploadService.ProcessUploadAsync(
                base64Image,
                userId,
                image.FileName,
                discordServerId,
                playerInGameId,
                enemyInGameId);

            // Return appropriate status code
            if (result.Success)
            {
                _logger.LogInformation(
                    "Upload processed successfully. UploadId: {UploadId}, IsDuplicate: {IsDuplicate}",
                    result.UploadId, result.IsDuplicate);
                return Ok(result);
            }
            else
            {
                // Check if it's a rate limit error
                // Currently checking error message for keywords, which is not ideal, we need better error typing
                if (result.ErrorMessage?.Contains("quota", StringComparison.OrdinalIgnoreCase) == true ||
                    result.ErrorMessage?.Contains("limit", StringComparison.OrdinalIgnoreCase) == true)
                {
                    return StatusCode(StatusCodes.Status429TooManyRequests, result);
                }

                return BadRequest(result);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during upload processing for user {UserId}", userId);
            #if DEBUG
                return BadRequest(new UploadResponse
                {
                    Success = false,
                    ErrorMessage = $"{ex.Message} | {ex.InnerException?.Message}"
                });
            #else
                return BadRequest(new UploadResponse
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred during upload processing"
                });
            #endif
        }
    }
}