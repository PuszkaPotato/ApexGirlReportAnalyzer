using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ApexGirlReportAnalyzer.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class AddTeamRankAndServerToBattleSide : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Server",
                table: "BattleSides",
                type: "integer",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "TeamRank",
                table: "BattleSides",
                type: "integer",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Server",
                table: "BattleSides");

            migrationBuilder.DropColumn(
                name: "TeamRank",
                table: "BattleSides");
        }
    }
}
