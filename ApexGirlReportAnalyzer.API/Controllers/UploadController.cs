using ApexGirlReportAnalyzer.API.Helpers;
using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Controllers;

[Authorize]
[ApiController]
[Route("api/[controller]")]
public class UploadController : ControllerBase
{
    private readonly IUploadService _uploadService;
    private readonly ILogger<UploadController> _logger;

    public UploadController(IUploadService uploadService, ILogger<UploadController> logger)
    {
        _uploadService = uploadService;
        _logger = logger;
    }

    /// <summary>
    /// Upload a single battle report screenshot for analysis
    /// </summary>
    /// <param name="image">Screenshot image file (PNG or JPEG)</param>
    /// <param name="userId">User ID making the upload</param>
    /// <param name="discordServerId">Optional: Discord server ID if uploaded via bot</param>
    /// <param name="playerInGameId">Optional: Player's in-game ID</param>
    /// <param name="enemyInGameId">Optional: Enemy's in-game ID</param>
    /// <param name="discordChannelId">Optional: Discord channel ID where screenshot was posted</param>
    /// <param name="discordMessageId">Optional: Discord message ID of the screenshot</param>
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
        [FromForm] string? enemyInGameId = null,
        [FromForm] string? discordChannelId = null,
        [FromForm] string? discordMessageId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate user ID
            var userIdValidation = UploadValidationHelper.ValidateUserId(userId);
            if (userIdValidation != null)
                return userIdValidation;

            // Validate image
            var imageValidation = UploadValidationHelper.ValidateImage(image);
            if (imageValidation != null)
                return imageValidation;

            _logger.LogInformation(
                "Processing upload for user {UserId}. File: {FileName}, Size: {Size} bytes",
                userId, image.FileName, image.Length);

            // Convert to base64
            var base64Image = await UploadValidationHelper.ConvertToBase64Async(image, cancellationToken);

            // Process upload
            var result = await _uploadService.ProcessUploadAsync(
                base64Image,
                userId,
                image.FileName,
                discordServerId,
                playerInGameId,
                enemyInGameId,
                discordChannelId,
                discordMessageId);

            // Return appropriate status code
            if (result.Success)
            {
                _logger.LogInformation(
                    "Upload processed successfully. UploadId: {UploadId}, IsDuplicate: {IsDuplicate}",
                    result.UploadId, result.IsDuplicate);
                return Ok(result);
            }

            var statusCode = UploadValidationHelper.GetStatusCodeForError(result.ErrorMessage);
            return StatusCode(statusCode, result);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during upload processing for user {UserId}", userId);
            return CreateErrorResponse(ex);
        }
    }

    /// <summary>
    /// Upload multiple battle report screenshots for batch analysis
    /// </summary>
    /// <param name="images">Screenshot image files (PNG or JPEG, max 20)</param>
    /// <param name="userId">User ID making the upload</param>
    /// <param name="discordServerId">Optional: Discord server ID if uploaded via bot</param>
    /// <returns>Batch analysis results or error</returns>
    [HttpPost("batch")]
    [ProducesResponseType(typeof(BatchUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(BatchUploadResponse), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(BatchUploadResponse), StatusCodes.Status429TooManyRequests)]
    public async Task<IActionResult> BatchUpload(
        IFormFileCollection images,
        [FromForm] Guid userId,
        [FromForm] string? discordServerId = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            // Validate user ID
            var userIdValidation = UploadValidationHelper.ValidateUserId(userId);
            if (userIdValidation is BadRequestObjectResult { Value: UploadResponse singleError })
            {
                return BadRequest(new BatchUploadResponse
                {
                    Success = false,
                    ErrorMessage = singleError.ErrorMessage
                });
            }
            else if (userIdValidation != null)
            {
                return BadRequest(new BatchUploadResponse
                {
                    Success = false,
                    ErrorMessage = "Valid user ID is required"
                });
            }

            // Validate batch
            var batchValidation = UploadValidationHelper.ValidateBatchUpload(images);
            if (batchValidation != null)
                return batchValidation;

            _logger.LogInformation(
                "Processing batch upload for user {UserId}. Count: {Count} images",
                userId, images.Count);

            // Process each image
            var results = new List<UploadResponse>();
            var successCount = 0;
            var failureCount = 0;
            var duplicateCount = 0;

            foreach (var image in images)
            {
                try
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    var base64Image = await UploadValidationHelper.ConvertToBase64Async(image, cancellationToken);

                    var result = await _uploadService.ProcessUploadAsync(
                        base64Image,
                        userId,
                        image.FileName,
                        discordServerId,
                        null, // No individual playerInGameId in batch
                        null); // No individual enemyInGameId in batch

                    results.Add(result);

                    if (result.Success)
                    {
                        successCount++;
                        if (result.IsDuplicate)
                            duplicateCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error processing image {FileName} in batch for user {UserId}",
                        image.FileName, userId);

                    results.Add(new UploadResponse
                    {
                        Success = false,
                        ErrorMessage = $"Failed to process {image.FileName}: {ex.Message}"
                    });
                    failureCount++;
                }
            }

            var batchResponse = new BatchUploadResponse
            {
                Success = successCount > 0, // Consider success if at least one upload succeeded
                TotalImages = images.Count,
                SuccessCount = successCount,
                FailureCount = failureCount,
                DuplicateCount = duplicateCount,
                Results = results
            };

            _logger.LogInformation(
                "Batch upload completed for user {UserId}. Success: {Success}, Failures: {Failures}, Duplicates: {Duplicates}",
                userId, successCount, failureCount, duplicateCount);

            // Return 429 if any quota errors, otherwise 200/400
            if (results.Any(r => UploadValidationHelper.GetStatusCodeForError(r.ErrorMessage) == StatusCodes.Status429TooManyRequests))
            {
                return StatusCode(StatusCodes.Status429TooManyRequests, batchResponse);
            }

            return batchResponse.Success ? Ok(batchResponse) : BadRequest(batchResponse);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during batch upload processing for user {UserId}", userId);
            return BadRequest(new BatchUploadResponse
            {
                Success = false,
                ErrorMessage = CreateErrorMessage(ex)
            });
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Creates appropriate error response based on build configuration
    /// </summary>
    private IActionResult CreateErrorResponse(Exception ex)
    {
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

    /// <summary>
    /// Creates error message based on build configuration
    /// </summary>
    private static string CreateErrorMessage(Exception ex)
    {
#if DEBUG
        return $"{ex.Message} | {ex.InnerException?.Message}";
#else
        return "An unexpected error occurred during batch upload processing";
#endif
    }

    #endregion
}