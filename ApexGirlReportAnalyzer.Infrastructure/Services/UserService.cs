using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApexGirlReportAnalyzer.Infrastructure.Services;

public class UserService(AppDbContext context, ILogger<UserService> logger) : IUserService
{
    private readonly AppDbContext _context = context;
    private readonly ILogger<UserService> _logger = logger;

    public async Task<QuotaInfo> GetRemainingQuotaAsync(Guid userId)
    {
        var user = await _context.Users
            .Include(u => u.Tier)
                .ThenInclude(t => t.TierLimits)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found or deleted", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        var userLimit = user.Tier.TierLimits
            .FirstOrDefault(tl => tl.Scope == TierScope.User);

        if (userLimit == null)
        {
            _logger.LogWarning("No tier limits found for user {UserId}, tier {TierId}", userId, user.TierId);
            return new QuotaInfo
            {
                DailyRemaining = 0,
                MonthlyRemaining = 0,
                TierName = user.Tier.Name
            };
        }

        var (dailyUploads, monthlyUploads) = await GetUploadCountsAsync(userId);

        _logger.LogInformation(
            "User {UserId} quota check - Daily: {Daily}/{DailyLimit}, Monthly: {Monthly}/{MonthlyLimit}",
            userId, dailyUploads, userLimit.DailyRequestLimit, monthlyUploads, userLimit.MonthlyRequestLimit);

        return new QuotaInfo
        {
            DailyRemaining = Math.Max(0, userLimit.DailyRequestLimit - dailyUploads),
            MonthlyRemaining = Math.Max(0, userLimit.MonthlyRequestLimit - monthlyUploads),
            TierName = user.Tier.Name
        };
    }

    public async Task<QuotaValidationResult> ValidateQuotaAsync(Guid userId)
    {
        return await ValidateQuotaAsync(userId, null);
    }

    public async Task<QuotaValidationResult> ValidateQuotaAsync(Guid userId, Guid? discordServerId)
    {
        var quota = await GetRemainingQuotaAsync(userId);

        // Check user quota first
        if (quota.DailyRemaining <= 0)
        {
            return new QuotaValidationResult
            {
                IsValid = false,
                QuotaInfo = quota,
                ErrorMessage = "Daily upload quota exceeded. Please try again tomorrow."
            };
        }

        if (quota.MonthlyRemaining <= 0)
        {
            return new QuotaValidationResult
            {
                IsValid = false,
                QuotaInfo = quota,
                ErrorMessage = "Monthly upload quota exceeded. Please upgrade your tier or wait until next month."
            };
        }

        // Check server quota if discordServerId is provided
        if (discordServerId.HasValue)
        {
            var serverQuotaResult = await ValidateServerQuotaAsync(discordServerId.Value, quota);
            if (!serverQuotaResult.IsValid)
            {
                return serverQuotaResult;
            }

            // Merge server quota info into response
            quota = serverQuotaResult.QuotaInfo;
        }

        return new QuotaValidationResult
        {
            IsValid = true,
            QuotaInfo = quota
        };
    }

    public async Task<bool> HasQuotaAsync(Guid userId)
    {
        var quota = await GetRemainingQuotaAsync(userId);
        return quota.DailyRemaining > 0 && quota.MonthlyRemaining > 0;
    }

    private async Task<QuotaValidationResult> ValidateServerQuotaAsync(Guid discordServerId, QuotaInfo userQuota)
    {
        var server = await _context.DiscordServers
            .Include(s => s.Tier)
                .ThenInclude(t => t!.TierLimits)
            .FirstOrDefaultAsync(s => s.Id == discordServerId && s.DeletedAt == null);

        if (server == null)
        {
            _logger.LogWarning("Discord server {ServerId} not found or deleted", discordServerId);
            // Server not found - allow upload with just user quota
            return new QuotaValidationResult
            {
                IsValid = true,
                QuotaInfo = userQuota
            };
        }

        // If server has no tier assigned, use default behavior (no server limits)
        if (server.Tier == null)
        {
            _logger.LogInformation("Discord server {ServerId} has no tier assigned, skipping server quota check", discordServerId);
            return new QuotaValidationResult
            {
                IsValid = true,
                QuotaInfo = userQuota
            };
        }

        var serverLimit = server.Tier.TierLimits
            .FirstOrDefault(tl => tl.Scope == TierScope.Server);

        if (serverLimit == null)
        {
            _logger.LogWarning("No server tier limits found for server {ServerId}, tier {TierId}", discordServerId, server.ServerTierId);
            return new QuotaValidationResult
            {
                IsValid = true,
                QuotaInfo = userQuota
            };
        }

        var (dailyUploads, monthlyUploads) = await GetServerUploadCountsAsync(discordServerId);

        _logger.LogInformation(
            "Server {ServerId} quota check - Daily: {Daily}/{DailyLimit}, Monthly: {Monthly}/{MonthlyLimit}",
            discordServerId, dailyUploads, serverLimit.DailyRequestLimit, monthlyUploads, serverLimit.MonthlyRequestLimit);

        var serverDailyRemaining = Math.Max(0, serverLimit.DailyRequestLimit - dailyUploads);
        var serverMonthlyRemaining = Math.Max(0, serverLimit.MonthlyRequestLimit - monthlyUploads);

        // Update quota info with server data
        userQuota.ServerDailyRemaining = serverDailyRemaining;
        userQuota.ServerMonthlyRemaining = serverMonthlyRemaining;
        userQuota.ServerTierName = server.Tier.Name;

        if (serverDailyRemaining <= 0)
        {
            return new QuotaValidationResult
            {
                IsValid = false,
                QuotaInfo = userQuota,
                ErrorMessage = "This Discord server's daily upload quota has been exceeded. Please try again tomorrow."
            };
        }

        if (serverMonthlyRemaining <= 0)
        {
            return new QuotaValidationResult
            {
                IsValid = false,
                QuotaInfo = userQuota,
                ErrorMessage = "This Discord server's monthly upload quota has been exceeded. Please contact the server admin to upgrade the tier."
            };
        }

        return new QuotaValidationResult
        {
            IsValid = true,
            QuotaInfo = userQuota
        };
    }

    private async Task<(int daily, int monthly)> GetUploadCountsAsync(Guid userId)
    {
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var dailyUploads = await _context.Uploads
            .Where(u => u.UserId == userId
                && u.Status == UploadStatus.Success
                && u.CreatedAt >= today
                && u.DeletedAt == null)
            .CountAsync();

        var monthlyUploads = await _context.Uploads
            .Where(u => u.UserId == userId
                && u.Status == UploadStatus.Success
                && u.CreatedAt >= startOfMonth
                && u.DeletedAt == null)
            .CountAsync();

        return (dailyUploads, monthlyUploads);
    }

    private async Task<(int daily, int monthly)> GetServerUploadCountsAsync(Guid discordServerId)
    {
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        var dailyUploads = await _context.Uploads
            .Where(u => u.DiscordServerId == discordServerId
                && u.Status == UploadStatus.Success
                && u.CreatedAt >= today
                && u.DeletedAt == null)
            .CountAsync();

        var monthlyUploads = await _context.Uploads
            .Where(u => u.DiscordServerId == discordServerId
                && u.Status == UploadStatus.Success
                && u.CreatedAt >= startOfMonth
                && u.DeletedAt == null)
            .CountAsync();

        return (dailyUploads, monthlyUploads);
    }
}
