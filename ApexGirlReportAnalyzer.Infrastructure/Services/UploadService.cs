using System.Security.Cryptography;
using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
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
        string? enemyInGameId = null)
    {
        Upload? upload = null;

        try
        {
            _logger.LogInformation("Starting upload processing for user {UserId}", userId);

            // Step 1: Validate user exists
            if (!await UserExistsAsync(userId))
            {
                _logger.LogWarning("User {UserId} not found or deleted", userId);
                return CreateErrorResponse("User not found");
            }

            // Step 2: Validate quota using UserService
            var quotaValidation = await _userService.ValidateQuotaAsync(userId);
            if (!quotaValidation.IsValid)
            {
                _logger.LogWarning("User {UserId} quota validation failed: {ErrorMessage}",
                    userId, quotaValidation.ErrorMessage);
                return CreateErrorResponse(
                    quotaValidation.ErrorMessage ?? "Quota exceeded",
                    quotaValidation.QuotaInfo);
            }

            // Step 3: Calculate image hash for deduplication
            var imageHash = CalculateImageHash(base64Image);
            _logger.LogInformation("Image hash calculated: {ImageHash}", imageHash);

            // Step 4: Check for duplicate
            var duplicateResponse = await CheckForDuplicateAsync(imageHash, quotaValidation.QuotaInfo);
            if (duplicateResponse != null)
            {
                return duplicateResponse;
            }

            // Step 5: Create Upload record with PENDING status
            upload = await CreatePendingUploadAsync(userId, imageHash, discordServerId);

            // Step 6: Call OpenAI service
            var battleData = await AnalyzeWithOpenAIAsync(upload, base64Image);
            if (battleData == null)
            {
                // Check if it was invalid image vs other error
                var errorMsg = upload.FailureReason?.Contains("Invalid image") == true
                    ? "This doesn't appear to be an Apex Girl battle report. Please upload a screenshot from the Battle Overview screen."
                    : "Failed to analyze screenshot. Please try again.";

                return CreateErrorResponse(
                    errorMsg,
                    quotaValidation.QuotaInfo,
                    upload.Id,
                    UploadStatus.Failed);
            }

            // Step 7-9: Save battle report and update upload to SUCCESS
            await SaveBattleReportAsync(upload, battleData, playerInGameId, enemyInGameId);

            _logger.LogInformation(
                "Upload {UploadId} processed successfully. Tokens: {Tokens}, Cost: €{Cost:F4}",
                upload.Id, upload.TokenEstimate, upload.EstimatedCostEuro);

            // Step 10: Return success response with updated quota
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
            await MarkUploadAsFailedAsync(upload, $"Unexpected error: {ex.Message}");

            return CreateErrorResponse(
                "An unexpected error occurred during processing",
                uploadId: upload?.Id,
                status: UploadStatus.Failed);
        }
    }

    #region Private Helper Methods

    /// <summary>
    /// Check if user exists and is not deleted
    /// </summary>
    private async Task<bool> UserExistsAsync(Guid userId)
    {
        return await _context.Users.AnyAsync(u => u.Id == userId && u.DeletedAt == null);
    }

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
            return null; // Not a duplicate
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
        string? discordServerId)
    {
        var model = _configuration["OpenAI:Model"] ?? "gpt-4o-mini";
        var promptVersion = _configuration["OpenAI:PromptVersion"] ?? "1.0";

        var upload = new Upload
        {
            Id = Guid.NewGuid(),
            ImageHash = imageHash,
            UserId = userId,
            DiscordServerId = ParseDiscordServerId(discordServerId, userId),
            Status = UploadStatus.Pending,
            OpenAiModel = model,
            PromptVersion = promptVersion,
            TokenEstimate = 0,
            CreatedAt = DateTime.UtcNow
        };

        _context.Uploads.Add(upload);
        await _context.SaveChangesAsync();

        _logger.LogInformation("Upload record created with ID {UploadId}, calling OpenAI...", upload.Id);
        return upload;
    }

    /// <summary>
    /// Parse Discord server ID safely
    /// </summary>
    private Guid? ParseDiscordServerId(string? discordServerId, Guid userId)
    {
        if (string.IsNullOrWhiteSpace(discordServerId))
        {
            return null;
        }

        if (Guid.TryParse(discordServerId, out var parsed))
        {
            return parsed;
        }

        _logger.LogWarning(
            "Invalid DiscordServerId format: {DiscordServerId} for user {UserId}",
            discordServerId, userId);
        return null;
    }

    /// <summary>
    /// Analyze screenshot with OpenAI, handling errors gracefully
    /// </summary>
    private async Task<BattleReportResponse?> AnalyzeWithOpenAIAsync(Upload upload, string base64Image)
    {
        try
        {
            var result = await _openAIService.AnalyzeScreenshotAsync(base64Image);

            // Check if OpenAI flagged image as invalid
            if (result.IsInvalid)
            {
                _logger.LogWarning(
                    "Upload {UploadId} rejected by OpenAI: {Reason}",
                    upload.Id, result.InvalidReason);

                await MarkUploadAsFailedAsync(
                    upload,
                    $"Invalid image: {result.InvalidReason}");

                return null;
            }

            return result;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "OpenAI analysis failed for upload {UploadId}", upload.Id);
            await MarkUploadAsFailedAsync(upload, $"OpenAI analysis failed: {ex.Message}");
            return null;
        }
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
        // Create BattleReport
        var battleReport = new BattleReport
        {
            Id = Guid.NewGuid(),
            UploadId = upload.Id,
            BattleType = battleData.BattleType,
            BattleDate = battleData.BattleDate,
            ExtractionVersion = ParseExtractionVersion(upload.PromptVersion),
            CreatedAt = DateTime.UtcNow
        };

        _context.BattleReports.Add(battleReport);

        // Create BattleSides (Player and Enemy)
        var playerSide = CreateBattleSide(battleReport.Id, BattleSideType.Player, battleData.Player, playerInGameId);
        var enemySide = CreateBattleSide(battleReport.Id, BattleSideType.Enemy, battleData.Enemy, enemyInGameId);

        _context.BattleSides.Add(playerSide);
        _context.BattleSides.Add(enemySide);

        // Update Upload to SUCCESS
        upload.Status = UploadStatus.Success;
        upload.TokenEstimate = battleData.TokensUsed ?? 0;
        upload.EstimatedCostEuro = battleData.EstimatedCost;

        await _context.SaveChangesAsync();
    }

    /// <summary>
    /// Parse extraction version from prompt version string
    /// </summary>
    private int ParseExtractionVersion(string promptVersion)
    {
        if (string.IsNullOrEmpty(promptVersion))
        {
            return 1;
        }

        var parts = promptVersion.Split('.');
        if (parts.Length > 0 && int.TryParse(parts[0], out var version))
        {
            return version;
        }

        return 1;
    }

    /// <summary>
    /// Calculate SHA-256 hash of base64 image for deduplication
    /// </summary>
    private string CalculateImageHash(string base64Image)
    {
        var imageBytes = Convert.FromBase64String(base64Image);
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(imageBytes);
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
    }

    /// <summary>
    /// Create a BattleSide entity from DTO
    /// </summary>
    private BattleSide CreateBattleSide(Guid battleReportId, BattleSideType sideType, BattleSideDto dto, string? manualInGameId = null)
    {
        return new BattleSide
        {
            Id = Guid.NewGuid(),
            BattleReportId = battleReportId,
            Side = sideType,
            Username = dto.Username ?? string.Empty,
            InGamePlayerId = manualInGameId ?? dto.InGamePlayerId,
            GroupTag = dto.GroupTag,
            Level = dto.Level,
            FanCount = dto.FanCount,
            LossCount = dto.LossCount,
            InjuredCount = dto.InjuredCount,
            RemainingCount = dto.RemainingCount,
            ReinforceCount = dto.ReinforceCount,
            Sing = dto.Sing,
            Dance = dto.Dance,
            ActiveSkill = dto.ActiveSkill,
            BasicAttackBonus = dto.BasicAttackBonus,
            ReduceBasicAttackDamage = dto.ReduceBasicAttackDamage,
            SkillBonus = dto.SkillBonus,
            SkillReduction = dto.SkillReduction,
            ExtraDamage = dto.ExtraDamage,
            CreatedAt = DateTime.UtcNow
        };
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

        var playerSide = battleReport.BattleSides.FirstOrDefault(bs => bs.Side == BattleSideType.Player);
        var enemySide = battleReport.BattleSides.FirstOrDefault(bs => bs.Side == BattleSideType.Enemy);

        return new BattleReportResponse
        {
            BattleType = battleReport.BattleType,
            BattleDate = battleReport.BattleDate,
            Player = MapToDto(playerSide),
            Enemy = MapToDto(enemySide),
            TokensUsed = battleReport.Upload.TokenEstimate,
            EstimatedCost = battleReport.Upload.EstimatedCostEuro
        };
    }

    /// <summary>
    /// Map BattleSide entity to DTO
    /// </summary>
    private BattleSideDto MapToDto(BattleSide? side)
    {
        if (side == null)
        {
            return new BattleSideDto();
        }

        return new BattleSideDto
        {
            Username = side.Username,
            InGamePlayerId = side.InGamePlayerId,
            GroupTag = side.GroupTag,
            Level = side.Level,
            FanCount = side.FanCount,
            LossCount = side.LossCount,
            InjuredCount = side.InjuredCount,
            RemainingCount = side.RemainingCount,
            ReinforceCount = side.ReinforceCount,
            Sing = side.Sing,
            Dance = side.Dance,
            ActiveSkill = side.ActiveSkill,
            BasicAttackBonus = side.BasicAttackBonus,
            ReduceBasicAttackDamage = side.ReduceBasicAttackDamage,
            SkillBonus = side.SkillBonus,
            SkillReduction = side.SkillReduction,
            ExtraDamage = side.ExtraDamage
        };
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