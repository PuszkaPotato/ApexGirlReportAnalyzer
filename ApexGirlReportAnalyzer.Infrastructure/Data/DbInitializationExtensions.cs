using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace ApexGirlReportAnalyzer.Infrastructure.Data;

public static class DbInitializationExtensions
{
    public static async Task InitializeDatabaseAsync(
        this IServiceProvider serviceProvider,
        IConfiguration configuration,
        string environment)
    {
        if (environment != "Development")
            return;

        using var scope = serviceProvider.CreateScope();
        var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<AppDbContext>>();

        try
        {
            // Optional wipe: set configuration key `Seed:WipeDatabase` to true in
            // appsettings.Development.json or an environment variable to drop & recreate DB on startup.
            var wipe = configuration.GetValue<bool>("Seed:WipeDatabase", false);
            if (wipe)
            {
                // Drop database and re-run migrations so we have a clean slate in development
                dbContext.Database.EnsureDeleted();
                dbContext.Database.Migrate();
            }

            var seederLogger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("DbSeeder");
            var testUserId = await DbSeeder.SeedAsync(dbContext, seederLogger, true);
            if (testUserId != null)
            {
                logger.LogInformation("Development test user available: {Id}", testUserId);
                // Also write to console so the value is visible in the debug output window
                Console.WriteLine($"Development test user available: {testUserId}");
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error while seeding the database");
            Console.WriteLine($"Error while seeding the database: {ex.Message}");
            throw;
        }
    }
}
