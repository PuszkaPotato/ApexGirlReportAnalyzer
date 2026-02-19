using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexGirlReportAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddDiscordBotFields : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ModeratorRoleIds",
                table: "DiscordServers");

            migrationBuilder.AddColumn<bool>(
                name: "IsDefault",
                table: "Tiers",
                type: "boolean",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<string>(
                name: "AllowedRoleId",
                table: "DiscordServers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "LogChannelId",
                table: "DiscordServers",
                type: "text",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAt",
                table: "DiscordServers",
                type: "timestamp with time zone",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "UploadChannelId",
                table: "DiscordServers",
                type: "text",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsDefault",
                table: "Tiers");

            migrationBuilder.DropColumn(
                name: "AllowedRoleId",
                table: "DiscordServers");

            migrationBuilder.DropColumn(
                name: "LogChannelId",
                table: "DiscordServers");

            migrationBuilder.DropColumn(
                name: "UpdatedAt",
                table: "DiscordServers");

            migrationBuilder.DropColumn(
                name: "UploadChannelId",
                table: "DiscordServers");

            migrationBuilder.AddColumn<string>(
                name: "ModeratorRoleIds",
                table: "DiscordServers",
                type: "text",
                nullable: false,
                defaultValue: "");
        }
    }
}
