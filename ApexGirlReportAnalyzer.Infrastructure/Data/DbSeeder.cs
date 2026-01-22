using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Models.Enums;

namespace ApexGirlReportAnalyzer.Infrastructure.Data;

public static class DbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        // Only seed if database is empty
        if (context.Tiers.Any())
        {
            return; // Already seeded
        }

        // Create Tiers
        var freeTier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Free"
        };

        var plusTier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Plus"
        };

        var proTier = new Tier
        {
            Id = Guid.NewGuid(),
            Name = "Pro"
        };

        context.Tiers.AddRange(freeTier, plusTier, proTier);

        // Create Tier Limits
        var tierLimits = new List<TierLimit>
        {
            // Free Tier - User Scope
            new TierLimit
            {
                Id = Guid.NewGuid(),
                TierId = freeTier.Id,
                Scope = TierScope.User,
                DailyRequestLimit = 10,
                MonthlyRequestLimit = 100
            },
            // Free Tier - Server Scope
            new TierLimit
            {
                Id = Guid.NewGuid(),
                TierId = freeTier.Id,
                Scope = TierScope.Server,
                DailyRequestLimit = 50,
                MonthlyRequestLimit = 500
            },
            // Plus Tier - User Scope
            new TierLimit
            {
                Id = Guid.NewGuid(),
                TierId = plusTier.Id,
                Scope = TierScope.User,
                DailyRequestLimit = 20,
                MonthlyRequestLimit = 300
            },
            // Plus Tier - Server Scope
            new TierLimit
            {
                Id = Guid.NewGuid(),
                TierId = plusTier.Id,
                Scope = TierScope.Server,
                DailyRequestLimit = 150,
                MonthlyRequestLimit = 1000
            },
            // Pro Tier - User Scope
            new TierLimit
            {
                Id = Guid.NewGuid(),
                TierId = proTier.Id,
                Scope = TierScope.User,
                DailyRequestLimit = 100,
                MonthlyRequestLimit = 800
            },
            // Pro Tier - Server Scope
            new TierLimit
            {
                Id = Guid.NewGuid(),
                TierId = proTier.Id,
                Scope = TierScope.Server,
                DailyRequestLimit = 500,
                MonthlyRequestLimit = 5000
            }
        };

        context.TierLimits.AddRange(tierLimits);

        // Create Test API Key (for development)
        var testApiKey = new ApiKey
        {
            Id = Guid.NewGuid(),
            Key = "dev-test-key-12345", // In production, this would be hashed!
            Name = "Development Test Key",
            Scope = "admin",
            IsActive = true
        };

        context.ApiKeys.Add(testApiKey);

        // Save everything
        await context.SaveChangesAsync();
    }
}