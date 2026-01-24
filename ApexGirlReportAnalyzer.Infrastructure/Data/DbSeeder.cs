using ApexGirlReportAnalyzer.Models.Entities;
using ApexGirlReportAnalyzer.Models.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace ApexGirlReportAnalyzer.Infrastructure.Data;

public static class DbSeeder
{
    // Returns the ID of the development test user if created or found, otherwise null.
    public static async Task<Guid?> SeedAsync(AppDbContext context, ILogger? logger = null, bool isDevelopment = false)
    {
        // Only seed if database is empty, but always ensure dev test user exists in DEBUG
        if (context.Tiers.Any())
        {
            if (isDevelopment)
            {
            // If tiers already exist, still ensure the development test user is present and log its ID
            return await EnsureTestUserAsync(context, logger);
            }
            return null; // Already seeded
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
        await context.SaveChangesAsync(); // Persist tiers, limits and api key so subsequent DB queries see them

        if (isDevelopment)
        {
            // Create test user in development
            return await EnsureTestUserAsync(context, logger);
        }

        // Save everything
        await context.SaveChangesAsync();

        // If we reached here in DEBUG and no test user was created/found above, return null.
        return null;
    }

    // Add this private helper method to eliminate duplication
    private static async Task<Guid?> EnsureTestUserAsync(AppDbContext context, ILogger? logger)
    {
        var existing = await context.Users.FirstOrDefaultAsync(u => u.DiscordId == "test_user");
        if (existing != null)
        {
            logger?.LogInformation("Test user exists: ID = {Id}, DiscordId = {DiscordId}", existing.Id, existing.DiscordId);
            Console.WriteLine($"Test user exists: ID = {existing.Id}, DiscordId = {existing.DiscordId}");
            return existing.Id;
        }

        var freeTierEntity = await context.Tiers.FirstOrDefaultAsync(t => t.Name == "Free");
        if (freeTierEntity != null)
        {
            var testUser = new User
            {
                Id = Guid.NewGuid(),
                DiscordId = "test_user",
                TierId = freeTierEntity.Id,
                CreatedAt = DateTime.UtcNow
            };

            context.Users.Add(testUser);
            await context.SaveChangesAsync();

            logger?.LogInformation("Test user created: ID = {Id}, DiscordId = {DiscordId}", testUser.Id, testUser.DiscordId);
            Console.WriteLine($"Test user created: ID = {testUser.Id}, DiscordId = {testUser.DiscordId}");
            return testUser.Id;
        }

        return null;
    }
}