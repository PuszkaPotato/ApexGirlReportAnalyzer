using ApexGirlReportAnalyzer.Models.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace ApexGirlReportAnalyzer.API.Helpers;

/// <summary>
/// Helper class for upload validation logic
/// Extracts common validation methods to avoid code duplication
/// </summary>
public static class UploadValidationHelper
{
    private static readonly HashSet<string> AllowedContentTypes = ["image/png", "image/jpeg", "image/jpg"];
    private const int MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB
    private const int MaxBatchSize = 20;

    /// <summary>
    /// Validates a single image file
    /// </summary>
    public static IActionResult? ValidateImage(IFormFile? image)
    {
        if (image == null || image.Length == 0)
        {
            return new BadRequestObjectResult(new UploadResponse
            {
                Success = false,
                ErrorMessage = "No image provided"
            });
        }

        if (!AllowedContentTypes.Contains(image.ContentType.ToLower()))
        {
            return new BadRequestObjectResult(new UploadResponse
            {
                Success = false,
                ErrorMessage = "Only PNG and JPEG images are allowed"
            });
        }

        if (image.Length > MaxFileSizeBytes)
        {
            return new BadRequestObjectResult(new UploadResponse
            {
                Success = false,
                ErrorMessage = $"Image too large (max {MaxFileSizeBytes / 1024 / 1024}MB)"
            });
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Validates batch upload request
    /// </summary>
    public static IActionResult? ValidateBatchUpload(IFormFileCollection? images)
    {
        if (images == null || images.Count == 0)
        {
            return new BadRequestObjectResult(new BatchUploadResponse
            {
                Success = false,
                ErrorMessage = "No images provided"
            });
        }

        if (images.Count > MaxBatchSize)
        {
            return new BadRequestObjectResult(new BatchUploadResponse
            {
                Success = false,
                ErrorMessage = $"Too many images. Maximum {MaxBatchSize} images per batch"
            });
        }

        // Validate each image
        foreach (var image in images)
        {
            var validationResult = ValidateImage(image);
            if (validationResult is BadRequestObjectResult { Value: UploadResponse singleError })
            {
                return new BadRequestObjectResult(new BatchUploadResponse
                {
                    Success = false,
                    ErrorMessage = $"Invalid image '{image.FileName}': {singleError.ErrorMessage}"
                });
            }
            else if (validationResult != null)
            {
                return new BadRequestObjectResult(new BatchUploadResponse
                {
                    Success = false,
                    ErrorMessage = $"Invalid image '{image.FileName}'"
                });
            }
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Validates user ID
    /// </summary>
    public static IActionResult? ValidateUserId(Guid userId)
    {
        if (userId == Guid.Empty)
        {
            return new BadRequestObjectResult(new UploadResponse
            {
                Success = false,
                ErrorMessage = "Valid user ID is required"
            });
        }

        return null; // Validation passed
    }

    /// <summary>
    /// Converts IFormFile to base64 string
    /// </summary>
    public static async Task<string> ConvertToBase64Async(IFormFile image, CancellationToken cancellationToken = default)
    {
        using var memoryStream = new MemoryStream();
        await image.CopyToAsync(memoryStream, cancellationToken);
        var imageBytes = memoryStream.ToArray();
        return Convert.ToBase64String(imageBytes);
    }

    /// <summary>
    /// Determines appropriate HTTP status code based on error message
    /// </summary>
    public static int GetStatusCodeForError(string? errorMessage)
    {
        if (string.IsNullOrEmpty(errorMessage))
            return StatusCodes.Status400BadRequest;

        // Check for rate limit / quota errors with more specific patterns
        if (errorMessage.Contains("quota exceeded", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("rate limit", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("upload limit", StringComparison.OrdinalIgnoreCase) ||
            errorMessage.Contains("too many requests", StringComparison.OrdinalIgnoreCase))
        {
            return StatusCodes.Status429TooManyRequests;
        }

        return StatusCodes.Status400BadRequest;
    }
}