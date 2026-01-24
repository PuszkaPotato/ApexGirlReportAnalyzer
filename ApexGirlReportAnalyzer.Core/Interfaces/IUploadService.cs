using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Core.Interfaces;

/// <summary>
/// Service for handling screenshot uploads and processing
/// </summary>
public interface IUploadService
{
    /// <summary>
    /// Process a screenshot upload
    /// </summary>
    /// <param name="base64Image">Base64-encoded image</param>
    /// <param name="userId">User ID making the upload</param>
    /// <param name="originalFileName">Original filename (optional)</param>
    /// <param name="discordServerId">Discord server ID if uploaded via Discord (optional)</param>
    /// <returns>Upload response with battle data or error</returns>
    Task<UploadResponse> ProcessUploadAsync(
        string base64Image,
        Guid userId,
        string? originalFileName = null,
        string? discordServerId = null,
        string? playerInGameId = null,
        string? enemyInGameId = null);
}