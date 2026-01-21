using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexGirlReportAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ApiKeys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Key = table.Column<string>(type: "text", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    Scope = table.Column<string>(type: "text", nullable: false),
                    IsActive = table.Column<bool>(type: "boolean", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ApiKeys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tiers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "text", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tiers", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "DiscordServers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordServerId = table.Column<string>(type: "text", nullable: false),
                    OwnerDiscordId = table.Column<string>(type: "text", nullable: false),
                    ModeratorRoleIds = table.Column<string>(type: "text", nullable: false),
                    DefaultReportPrivacy = table.Column<int>(type: "integer", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    ServerTierId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_DiscordServers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_DiscordServers_Tiers_ServerTierId",
                        column: x => x.ServerTierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                });

            migrationBuilder.CreateTable(
                name: "TierLimits",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Scope = table.Column<int>(type: "integer", nullable: false),
                    DailyRequestLimit = table.Column<int>(type: "integer", nullable: false),
                    MonthlyRequestLimit = table.Column<int>(type: "integer", nullable: false),
                    TierId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TierLimits", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TierLimits_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Users",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordId = table.Column<string>(type: "text", nullable: false),
                    InGamePlayerId = table.Column<string>(type: "text", nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    TierId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Users", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Users_Tiers_TierId",
                        column: x => x.TierId,
                        principalTable: "Tiers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "Uploads",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ImageHash = table.Column<string>(type: "text", nullable: false),
                    PrivacyScope = table.Column<int>(type: "integer", nullable: false),
                    Status = table.Column<int>(type: "integer", nullable: false),
                    FailureReason = table.Column<string>(type: "text", nullable: true),
                    OpenAiModel = table.Column<string>(type: "text", nullable: false),
                    PromptVersion = table.Column<string>(type: "text", nullable: false),
                    TokenEstimate = table.Column<int>(type: "integer", nullable: false),
                    EstimatedCostEuro = table.Column<decimal>(type: "numeric(10,6)", precision: 10, scale: 6, nullable: true),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordServerId = table.Column<Guid>(type: "uuid", nullable: true),
                    DiscordChannelId = table.Column<string>(type: "text", nullable: true),
                    DiscordMessageId = table.Column<string>(type: "text", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Uploads", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Uploads_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_Uploads_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "AnalyticsEvents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    EventType = table.Column<int>(type: "integer", nullable: false),
                    Metadata = table.Column<string>(type: "text", nullable: true),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    DiscordServerId = table.Column<Guid>(type: "uuid", nullable: true),
                    UploadId = table.Column<Guid>(type: "uuid", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AnalyticsEvents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_DiscordServers_DiscordServerId",
                        column: x => x.DiscordServerId,
                        principalTable: "DiscordServers",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Uploads_UploadId",
                        column: x => x.UploadId,
                        principalTable: "Uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.SetNull);
                    table.ForeignKey(
                        name: "FK_AnalyticsEvents_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "BattleReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ExtractionVersion = table.Column<int>(type: "integer", nullable: false),
                    BattleDate = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                    BattleType = table.Column<string>(type: "text", nullable: false),
                    DeletedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleReports_Uploads_UploadId",
                        column: x => x.UploadId,
                        principalTable: "Uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ErrorReports",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    ReportedIssue = table.Column<string>(type: "text", nullable: false),
                    CorrectedData = table.Column<string>(type: "text", nullable: true),
                    ResolvedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                    UploadId = table.Column<Guid>(type: "uuid", nullable: false),
                    UserId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ErrorReports", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ErrorReports_Uploads_UploadId",
                        column: x => x.UploadId,
                        principalTable: "Uploads",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_ErrorReports_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "BattleSides",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Side = table.Column<int>(type: "integer", nullable: false),
                    Username = table.Column<string>(type: "text", nullable: false),
                    InGamePlayerId = table.Column<string>(type: "text", nullable: true),
                    GroupTag = table.Column<string>(type: "text", nullable: true),
                    Level = table.Column<int>(type: "integer", nullable: true),
                    FanCount = table.Column<int>(type: "integer", nullable: false),
                    LossCount = table.Column<int>(type: "integer", nullable: false),
                    InjuredCount = table.Column<int>(type: "integer", nullable: false),
                    RemainingCount = table.Column<int>(type: "integer", nullable: true),
                    ReinforceCount = table.Column<int>(type: "integer", nullable: true),
                    Sing = table.Column<int>(type: "integer", nullable: false),
                    Dance = table.Column<int>(type: "integer", nullable: false),
                    ActiveSkill = table.Column<int>(type: "integer", nullable: false),
                    BasicAttackBonus = table.Column<int>(type: "integer", nullable: false),
                    ReduceBasicAttackDamage = table.Column<int>(type: "integer", nullable: false),
                    SkillBonus = table.Column<int>(type: "integer", nullable: false),
                    SkillReduction = table.Column<int>(type: "integer", nullable: false),
                    ExtraDamage = table.Column<int>(type: "integer", nullable: false),
                    BattleReportId = table.Column<Guid>(type: "uuid", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BattleSides", x => x.Id);
                    table.ForeignKey(
                        name: "FK_BattleSides_BattleReports_BattleReportId",
                        column: x => x.BattleReportId,
                        principalTable: "BattleReports",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_CreatedAt",
                table: "AnalyticsEvents",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_DiscordServerId",
                table: "AnalyticsEvents",
                column: "DiscordServerId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_EventType",
                table: "AnalyticsEvents",
                column: "EventType");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_UploadId",
                table: "AnalyticsEvents",
                column: "UploadId");

            migrationBuilder.CreateIndex(
                name: "IX_AnalyticsEvents_UserId",
                table: "AnalyticsEvents",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_IsActive_ExpiresAt",
                table: "ApiKeys",
                columns: new[] { "IsActive", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_ApiKeys_Key",
                table: "ApiKeys",
                column: "Key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleReports_BattleDate",
                table: "BattleReports",
                column: "BattleDate");

            migrationBuilder.CreateIndex(
                name: "IX_BattleReports_BattleType",
                table: "BattleReports",
                column: "BattleType");

            migrationBuilder.CreateIndex(
                name: "IX_BattleReports_UploadId",
                table: "BattleReports",
                column: "UploadId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_BattleSides_BattleReportId",
                table: "BattleSides",
                column: "BattleReportId");

            migrationBuilder.CreateIndex(
                name: "IX_BattleSides_GroupTag",
                table: "BattleSides",
                column: "GroupTag");

            migrationBuilder.CreateIndex(
                name: "IX_BattleSides_Side",
                table: "BattleSides",
                column: "Side");

            migrationBuilder.CreateIndex(
                name: "IX_BattleSides_Username",
                table: "BattleSides",
                column: "Username");

            migrationBuilder.CreateIndex(
                name: "IX_DiscordServers_DiscordServerId",
                table: "DiscordServers",
                column: "DiscordServerId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_DiscordServers_ServerTierId",
                table: "DiscordServers",
                column: "ServerTierId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_ResolvedAt",
                table: "ErrorReports",
                column: "ResolvedAt");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_UploadId",
                table: "ErrorReports",
                column: "UploadId");

            migrationBuilder.CreateIndex(
                name: "IX_ErrorReports_UserId",
                table: "ErrorReports",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_TierLimits_TierId_Scope",
                table: "TierLimits",
                columns: new[] { "TierId", "Scope" });

            migrationBuilder.CreateIndex(
                name: "IX_Tiers_Name",
                table: "Tiers",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_CreatedAt",
                table: "Uploads",
                column: "CreatedAt");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_DiscordServerId",
                table: "Uploads",
                column: "DiscordServerId");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_ImageHash",
                table: "Uploads",
                column: "ImageHash");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_Status",
                table: "Uploads",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Uploads_UserId",
                table: "Uploads",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Users_DiscordId",
                table: "Users",
                column: "DiscordId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Users_TierId",
                table: "Users",
                column: "TierId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AnalyticsEvents");

            migrationBuilder.DropTable(
                name: "ApiKeys");

            migrationBuilder.DropTable(
                name: "BattleSides");

            migrationBuilder.DropTable(
                name: "ErrorReports");

            migrationBuilder.DropTable(
                name: "TierLimits");

            migrationBuilder.DropTable(
                name: "BattleReports");

            migrationBuilder.DropTable(
                name: "Uploads");

            migrationBuilder.DropTable(
                name: "DiscordServers");

            migrationBuilder.DropTable(
                name: "Users");

            migrationBuilder.DropTable(
                name: "Tiers");
        }
    }
}
