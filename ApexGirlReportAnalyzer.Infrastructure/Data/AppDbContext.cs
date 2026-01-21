using ApexGirlReportAnalyzer.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace ApexGirlReportAnalyzer.Infrastructure.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    // DbSets - each represents a table
    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Tier> Tiers { get; set; } = null!;
    public DbSet<TierLimit> TierLimits { get; set; } = null!;
    public DbSet<ApiKey> ApiKeys { get; set; } = null!;
    public DbSet<DiscordServer> DiscordServers { get; set; } = null!;
    public DbSet<Upload> Uploads { get; set; } = null!;
    public DbSet<BattleReport> BattleReports { get; set; } = null!;
    public DbSet<BattleSide> BattleSides { get; set; } = null!;
    public DbSet<AnalyticsEvent> AnalyticsEvents { get; set; } = null!;
    public DbSet<ErrorReport> ErrorReports { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // We'll configure entities here next
        ConfigureUser(modelBuilder);
        ConfigureTier(modelBuilder);
        ConfigureTierLimit(modelBuilder);
        ConfigureApiKey(modelBuilder);
        ConfigureDiscordServer(modelBuilder);
        ConfigureUpload(modelBuilder);
        ConfigureBattleReport(modelBuilder);
        ConfigureBattleSide(modelBuilder);
        ConfigureAnalyticsEvent(modelBuilder);
        ConfigureErrorReport(modelBuilder);
    }

    // Configuration methods (we'll fill these in next)
    private void ConfigureUser(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
        {
            // Unique index on DiscordId
            entity.HasIndex(e => e.DiscordId).IsUnique();

            // Relationship to Tier (required)
            entity.HasOne(e => e.Tier)
                  .WithMany(t => t.Users)
                  .HasForeignKey(e => e.TierId)
                  .OnDelete(DeleteBehavior.Restrict); // Don't cascade delete users if tier deleted

            // Soft delete filter
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }

    private void ConfigureTier(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Tier>(entity =>
        {
            // Unique index on Name
            entity.HasIndex(e => e.Name).IsUnique();
        });
    }

    private void ConfigureTierLimit(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TierLimit>(entity =>
        {
            // Composite index on TierId and Scope
            entity.HasIndex(e => new { e.TierId, e.Scope });

            // Relationship to Tier
            entity.HasOne(e => e.Tier)
                  .WithMany(t => t.TierLimits)
                  .HasForeignKey(e => e.TierId)
                  .OnDelete(DeleteBehavior.Cascade); // Delete limits if tier deleted
        });
    }

    private void ConfigureApiKey(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ApiKey>(entity =>
        {
            // Unique index on Key
            entity.HasIndex(e => e.Key).IsUnique();

            // Composite index for active key lookups
            entity.HasIndex(e => new { e.IsActive, e.ExpiresAt });
        });
    }

    private void ConfigureDiscordServer(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<DiscordServer>(entity =>
        {
            // Unique index on DiscordServerId
            entity.HasIndex(e => e.DiscordServerId).IsUnique();

            // Index on ServerTierId
            entity.HasIndex(e => e.ServerTierId);

            // Relationship to Tier (optional)
            entity.HasOne(e => e.Tier)
                  .WithMany(t => t.DiscordServers)
                  .HasForeignKey(e => e.ServerTierId)
                  .OnDelete(DeleteBehavior.SetNull); // Set null if tier deleted

            // Soft delete filter
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }

    private void ConfigureUpload(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Upload>(entity =>
        {
            // Index on ImageHash for deduplication
            entity.HasIndex(e => e.ImageHash);

            // Index on UserId
            entity.HasIndex(e => e.UserId);

            // Index on DiscordServerId
            entity.HasIndex(e => e.DiscordServerId);

            // Index on Status for queue processing
            entity.HasIndex(e => e.Status);

            // Index on CreatedAt for time-series queries
            entity.HasIndex(e => e.CreatedAt);

            // Relationship to User (required)
            entity.HasOne(e => e.User)
                  .WithMany(u => u.Uploads)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);

            // Relationship to DiscordServer (optional)
            entity.HasOne(e => e.DiscordServer)
                  .WithMany(ds => ds.Uploads)
                  .HasForeignKey(e => e.DiscordServerId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Decimal precision for EstimatedCostEuro
            entity.Property(e => e.EstimatedCostEuro)
                  .HasPrecision(10, 6); // 10 total digits, 4 after decimal

            // Soft delete filter
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }

    private void ConfigureBattleReport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BattleReport>(entity =>
        {
            // Unique index on UploadId (one-to-one)
            entity.HasIndex(e => e.UploadId).IsUnique();

            // Index on BattleDate for time-series
            entity.HasIndex(e => e.BattleDate);

            // Index on BattleType for filtering
            entity.HasIndex(e => e.BattleType);

            // Relationship to Upload (one-to-one)
            entity.HasOne(e => e.Upload)
                  .WithOne(u => u.BattleReport)
                  .HasForeignKey<BattleReport>(e => e.UploadId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Soft delete filter
            entity.HasQueryFilter(e => e.DeletedAt == null);
        });
    }

    private void ConfigureBattleSide(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<BattleSide>(entity =>
        {
            // Index on BattleReportId
            entity.HasIndex(e => e.BattleReportId);

            // Index on Side
            entity.HasIndex(e => e.Side);

            // Index on Username for player analytics
            entity.HasIndex(e => e.Username);

            // Index on GroupTag for alliance analytics
            entity.HasIndex(e => e.GroupTag);

            // Relationship to BattleReport
            entity.HasOne(e => e.BattleReport)
                  .WithMany(br => br.BattleSides)
                  .HasForeignKey(e => e.BattleReportId)
                  .OnDelete(DeleteBehavior.Cascade);
        });
    }

    private void ConfigureAnalyticsEvent(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AnalyticsEvent>(entity =>
        {
            // Index on EventType
            entity.HasIndex(e => e.EventType);

            // Index on CreatedAt
            entity.HasIndex(e => e.CreatedAt);

            // Index on UserId
            entity.HasIndex(e => e.UserId);

            // Index on DiscordServerId
            entity.HasIndex(e => e.DiscordServerId);

            // Relationship to User (required)
            entity.HasOne(e => e.User)
                  .WithMany(u => u.AnalyticsEvents)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relationship to DiscordServer (optional)
            entity.HasOne(e => e.DiscordServer)
                  .WithMany(ds => ds.AnalyticsEvents)
                  .HasForeignKey(e => e.DiscordServerId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Relationship to Upload (optional)
            entity.HasOne(e => e.Upload)
                  .WithMany(u => u.AnalyticsEvents)
                  .HasForeignKey(e => e.UploadId)
                  .OnDelete(DeleteBehavior.SetNull);
        });
    }

    private void ConfigureErrorReport(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<ErrorReport>(entity =>
        {
            // Index on UploadId
            entity.HasIndex(e => e.UploadId);

            // Index on UserId
            entity.HasIndex(e => e.UserId);

            // Index on ResolvedAt for filtering
            entity.HasIndex(e => e.ResolvedAt);

            // Relationship to Upload (required)
            entity.HasOne(e => e.Upload)
                  .WithMany(u => u.ErrorReports)
                  .HasForeignKey(e => e.UploadId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Relationship to User (required)
            entity.HasOne(e => e.User)
                  .WithMany(u => u.ErrorReports)
                  .HasForeignKey(e => e.UserId)
                  .OnDelete(DeleteBehavior.Restrict);
        });
    }
}