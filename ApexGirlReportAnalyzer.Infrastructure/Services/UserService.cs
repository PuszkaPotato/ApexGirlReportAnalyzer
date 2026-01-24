using ApexGirlReportAnalyzer.Core.Interfaces;
using ApexGirlReportAnalyzer.Infrastructure.Data;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApexGirlReportAnalyzer.Infrastructure.Services;

public class UserService : IUserService
{
    private readonly AppDbContext _context;
    private readonly ILogger<UserService> _logger;

    public UserService(AppDbContext context, ILogger<UserService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<QuotaInfo> GetRemainingQuotaAsync(Guid userId)
    {
        // Load user with tier and limits
        var user = await _context.Users
            .Include(u => u.Tier)
                .ThenInclude(t => t.TierLimits)
            .FirstOrDefaultAsync(u => u.Id == userId && u.DeletedAt == null);

        if (user == null)
        {
            _logger.LogWarning("User {UserId} not found or deleted", userId);
            throw new KeyNotFoundException($"User with ID {userId} not found");
        }

        // Get tier limits for User scope (not Server scope)
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

        // Calculate date ranges
        var today = DateTime.SpecifyKind(DateTime.UtcNow.Date, DateTimeKind.Utc);
        var startOfMonth = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, 1, 0, 0, 0, DateTimeKind.Utc);

        // Count successful uploads (don't count failed or pending)
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
        var quota = await GetRemainingQuotaAsync(userId);

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
}