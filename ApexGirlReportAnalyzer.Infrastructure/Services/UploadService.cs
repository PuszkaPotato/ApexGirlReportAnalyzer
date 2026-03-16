using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Infrastructure.Helpers;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ApexGirlReportAnalyzer.Infrastructure.Services;

public class UploadService : IUploadService
{
    private readonly AppDbContext _context;
    private readonly IOpenAIService _openAIService;
    private readonly IUserService _userService;
    private readonly IBattleReportService _battleReportService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<UploadService> _logger;

    public UploadService(
        AppDbContext context,
        IOpenAIService openAIService,
        IUserService userService,
        IBattleReportService battleReportService,
        IConfiguration configuration,
        ILogger<UploadService> logger)
    {
        _context = context;
        _openAIService = openAIService;
        _userService = userService;
        _battleReportService = battleReportService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<UploadResponse> ProcessUploadAsync(
        string base64Image,
        Guid userId,
        string? originalFileName = null,
        string? discordServerId = null,
        string? playerInGameId = null,
        string? enemyInGameId = null,
        string? discordChannelId = null,
        string? discordMessageId = null,
        int? playerTeamRank = null,
        int? enemyTeamRank = null,
        int? playerServer = null,
        int? enemyServer = null)
    {
        Upload? upload = null;

        try
        {
            _logger.LogInformation("Starting upload processing for user {UserId}", userId);

            if (!await _userService.UserExistsAsync(userId))
            {
                _logger.LogWarning("User {UserId} not found or deleted", userId);
                return CreateErrorResponse("User not found");
            }

            var quotaValidation = await _userService.ValidateQuotaAsync(userId, discordServerId);
            if (!quotaValidation.IsValid)
            {
                _logger.LogWarning("User {UserId} quota validation failed: {ErrorMessage}",
                    userId, quotaValidation.ErrorMessage);
                return CreateErrorResponse(
                    quotaValidation.ErrorMessage ?? "Quota exceeded",
                    quotaValidation.QuotaInfo);
            }

            var imageHash = HashHelper.CalculateSha256(base64Image);
            _logger.LogInformation("Image hash calculated: {ImageHash}", imageHash);

            var duplicateResponse = await CheckForDuplicateAsync(imageHash, quotaValidation.QuotaInfo);
            if (duplicateResponse != null)
            {
                return duplicateResponse;
            }

            upload = await CreatePendingUploadAsync(userId, imageHash, discordServerId, discordChannelId, discordMessageId);

            var battleData = await _openAIService.AnalyzeScreenshotAsync(base64Image);

            if (battleData.IsInvalid)
            {
                await MarkUploadAsFailedAsync(upload, battleData.InvalidReason ?? "Failed to retrieve the reason from OpenAi");
                return CreateErrorResponse(
                    battleData.InvalidReason ?? "Invalid screenshot",
                    quotaValidation.QuotaInfo,
                    upload.Id,
                    UploadStatus.Failed);
            }

            var reportId = await _battleReportService.CreateBattleReportAsync(battleData, upload.Id, playerInGameId, enemyInGameId, playerTeamRank, enemyTeamRank, playerServer, enemyServer);

            await SaveUploadAsync(upload, battleData);

            battleData = await _battleReportService.GetBattleReportByIdAsync(reportId);

            _logger.LogInformation(
                "Upload {UploadId} processed successfully. Tokens: {Tokens}, Cost: €{Cost:F4}",
                upload.Id, upload.TokenEstimate, upload.EstimatedCostEuro);

            var updatedQuota = await _userService.GetRemainingQuotaAsync(userId);

            return new UploadResponse
            {
                Success = true,
                UploadId = upload.Id,
                Status = UploadStatus.Success,
                BattleData = battleData,
                IsDuplicate = false,
                RemainingQuota = updatedQuota
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error processing upload for user {UserId}", userId);

            if (upload != null)
                await MarkUploadAsFailedAsync(upload, $"Unexpected error: {ex.Message}");

            return CreateErrorResponse(
                "An unexpected error occurred during processing",
                uploadId: upload?.Id,
                status: UploadStatus.Failed);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Check if this image has already been processed
    /// </summary>
    private async Task<UploadResponse?> CheckForDuplicateAsync(string imageHash, QuotaInfo quotaInfo)
    {
        var existingUpload = await _context.Uploads
            .Include(u => u.BattleReport)
            .FirstOrDefaultAsync(u => u.ImageHash == imageHash
                && u.DeletedAt == null
                && u.Status == UploadStatus.Success);

        if (existingUpload?.BattleReport == null)
        {
            return null;
        }

        _logger.LogInformation(
            "Duplicate image detected. Returning existing battle report {BattleReportId}",
            existingUpload.BattleReport.Id);

        return new UploadResponse
        {
            Success = true,
            UploadId = existingUpload.Id,
            Status = UploadStatus.Success,
            IsDuplicate = true,
            BattleData = await _battleReportService.GetBattleReportByIdAsync(existingUpload.BattleReport.Id),
            RemainingQuota = quotaInfo // Quota not consumed for duplicates
        };
    }

    /// <summary>
    /// Create a pending upload record
    /// </summary>
    private async Task<Upload> CreatePendingUploadAsync(
        Guid userId,
        string imageHash,
        string? discordServerId,
        string? discordChannelId = null,
        string? discordMessageId = null)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";

        var upload = new Upload
        {
            Id = Guid.NewGuid(),
            ImageHash = imageHash,
            UserId = userId,
            DiscordServerId = discordServerId,
            DiscordChannelId = discordChannelId,
            DiscordMessageId = discordMessageId,
            Status = UploadStatus.Pending,
            OpenAiModel = model,
            TokenEstimate = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Uploads.Add(upload);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Upload record created with ID {UploadId}, calling OpenAI...", upload.Id);
        return upload;
    }

    /// <summary>
    /// Mark upload as failed and save
    /// </summary>
    private async Task MarkUploadAsFailedAsync(Upload? upload, string reason)
    {
        if (upload == null) return;

        try
        {
            upload.Status = UploadStatus.Failed;
            upload.FailureReason = reason;
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to save error status for upload {UploadId}", upload.Id);
        }
    }

    /// <summary>
    /// Save battle report and related entities
    /// </summary>
    private async Task SaveUploadAsync(
    Upload upload,
    BattleReportResponse battleData)
    {
        upload.Status = UploadStatus.Success;
        upload.TokenEstimate = battleData.TokensUsed ?? 0;
        upload.EstimatedCostEuro = battleData.EstimatedCost;
        upload.PromptVersion = battleData.PromptVersion;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Create an error response
    /// </summary>
    private UploadResponse CreateErrorResponse(
        string errorMessage,
        QuotaInfo? quotaInfo = null,
        Guid? uploadId = null,
        UploadStatus status = UploadStatus.Failed)
    {
        return new UploadResponse
        {
            Success = false,
            ErrorMessage = errorMessage,
            RemainingQuota = quotaInfo,
            UploadId = uploadId,
            Status = status
        };
    }

    #endregion
}