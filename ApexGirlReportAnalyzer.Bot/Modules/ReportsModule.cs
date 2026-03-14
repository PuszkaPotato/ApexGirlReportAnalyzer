using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;

namespace ApexGirlReportAnalyzer.Bot.Modules;

public class ReportsModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly ReportsService _reportsService;

    public ReportsModule(ReportsService reportsService)
    {
        _reportsService = reportsService;
    }

    [SlashCommand("reports", "View recent battle reports.")]
    public async Task ReportsAsync(
        [Summary("participant", "Filter by player name")] string? participant = null,
        [Summary("limit", "Number of reports to show (max 10)")] int limit = 5)
    {
        limit = Math.Clamp(limit, 1, 10);

        var result = await _reportsService.GetReportsAsync(participant: participant, limit: limit);

        if (result == null || result.BattleReports.Count == 0)
        {
            await RespondAsync("No battle reports found.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Recent Battle Reports")
            .WithColor(Color.Gold)
            .WithFooter($"Showing {result.Count} of {result.TotalCount} reports");

        foreach (var report in result.BattleReports)
        {
            var playerName = report.Player?.Username ?? report.Player?.InGamePlayerId ?? "Unknown";
            var enemyName = report.Enemy?.Username ?? report.Enemy?.InGamePlayerId ?? "Unknown";
            var date = report.BattleDate.ToString("yyyy-MM-dd");

            embed.AddField(
                $"{playerName} vs {enemyName}",
                $"**Type:** {report.BattleType} | **Date:** {date}",
                inline: false);
        }

        await RespondAsync(embed: embed.Build());
    }
}
