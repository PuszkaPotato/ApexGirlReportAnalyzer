using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Infrastructure.Helpers;
using ApexGirlReportAnalyzer.Infrastructure.Mappers;
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
    private readonly IConfiguration _configuration;
    private readonly ILogger<UploadService> _logger;

    public UploadService(
        AppDbContext context,
        IOpenAIService openAIService,
        IUserService userService,
        IConfiguration configuration,
        ILogger<UploadService> logger)
    {
        _context = context;
        _openAIService = openAIService;
        _userService = userService;
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
        string? discordMessageId = null)
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

            await SaveBattleReportAsync(upload, battleData, playerInGameId, enemyInGameId);

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
                TokensUsed = battleData.TokensUsed,
                EstimatedCost = battleData.EstimatedCost,
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
            ExistingBattleReportId = existingUpload.BattleReport.Id,
            BattleData = await GetBattleReportResponseAsync(existingUpload.BattleReport.Id),
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
    private async Task SaveBattleReportAsync(
    Upload upload,
    BattleReportResponse battleData,
    string? playerInGameId = null,
    string? enemyInGameId = null)
    {
        var battleReport = new BattleReport
        {
            Id = Guid.NewGuid(),
            UploadId = upload.Id,
            BattleType = battleData.BattleType,
            BattleDate = battleData.BattleDate,
            CreatedAt = DateTime.UtcNow
        };

        _context.BattleReports.Add(battleReport);

        var playerSide = BattleReportMapper.ToEntity(battleData.Player, battleReport.Id, BattleSideType.Player, playerInGameId);
        var enemySide = BattleReportMapper.ToEntity(battleData.Enemy, battleReport.Id, BattleSideType.Enemy, enemyInGameId);

        _context.BattleSides.Add(playerSide);
        _context.BattleSides.Add(enemySide);

        upload.Status = UploadStatus.Success;
        upload.TokenEstimate = battleData.TokensUsed ?? 0;
        upload.EstimatedCostEuro = battleData.EstimatedCost;
        upload.PromptVersion = battleData.PromptVersion;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Retrieve full battle report data by ID (for duplicate detection)
    /// </summary>
    private async Task<BattleReportResponse> GetBattleReportResponseAsync(Guid battleReportId)
    {
        var battleReport = await _context.BattleReports
            .Include(br => br.BattleSides)
            .Include(br => br.Upload)
            .FirstOrDefaultAsync(br => br.Id == battleReportId);

        if (battleReport == null)
        {
            throw new InvalidOperationException($"BattleReport {battleReportId} not found");
        }

        return BattleReportMapper.ToDto(battleReport, battleReport.Upload);
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