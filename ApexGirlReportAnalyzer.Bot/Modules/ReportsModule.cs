using ApexGirlReportAnalyzer.Bot.Handlers;
using ApexGirlReportAnalyzer.Bot.Services;
using ApexGirlReportAnalyzer.Models.DTOs;
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
        [Summary("battle-type", "Filter by battle type")] string? battleType = null,
        [Summary("group-tag", "Filter by group tag")] string? groupTag = null,
        [Summary("in-game-id", "Filter by in-game player ID")] string? inGameId = null,
        [Summary("battle-date", "Filter by battle date (yyyy-MM-dd)")] string? battleDate = null,
        [Summary("limit", "Number of reports to show (max 10)")] int limit = 10)
    {
        await DeferAsync();

        limit = Math.Clamp(limit, 1, 10);

        DateTime? parsedDate = null;
        if (battleDate != null && DateTime.TryParse(battleDate, out var d))
            parsedDate = d;

        var result = await _reportsService.GetReportsAsync(participant: participant, battleType: battleType, groupTag: groupTag, inGameId: inGameId, battleDate: parsedDate, limit: limit);

        if (result == null || result.BattleReports.Count == 0)
        {
            await FollowupAsync("No battle reports found.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Recent Battle Reports")
            .WithColor(Color.Gold)
            .WithFooter($"Showing {result.Count} of {result.TotalCount} reports");

        for (int i = 0; i < result.BattleReports.Count; i++)
        {
            var report = result.BattleReports[i];
            var playerName = report.Player?.Username ?? report.Player?.InGamePlayerId ?? "Unknown";
            var enemyName = report.Enemy?.Username ?? report.Enemy?.InGamePlayerId ?? "Unknown";

            embed.AddField(
                $"{i + 1}. {playerName} vs {enemyName}",
                $"**Type:** {report.BattleType} | **Date:** {report.BattleDate:yyyy-MM-dd}",
                inline: false);
        }

        var components = BuildReportButtons(result.BattleReports);

        await FollowupAsync(embed: embed.Build(), components: components.Build());
    }

    [SlashCommand("export", "Export battle reports as a CSV file.")]
    public async Task ExportAsync(
        [Summary("participant", "Filter by player name")] string? participant = null,
        [Summary("battle-type", "Filter by battle type")] string? battleType = null,
        [Summary("group-tag", "Filter by group tag")] string? groupTag = null)
    {
        await DeferAsync(ephemeral: true);

        var stream = await _reportsService.ExportReportsCsvAsync(
            requestingDiscordUserId: Context.User.Id.ToString(),
            participant: participant,
            battleType: battleType,
            groupTag: groupTag);

        if (stream == null)
        {
            await FollowupAsync("Failed to export reports. Please try again.", ephemeral: true);
            return;
        }

        var fileName = $"battle-reports-{DateTime.UtcNow:yyyy-MM-dd}.csv";
        await FollowupWithFileAsync(stream, fileName, ephemeral: true);
    }

    [ComponentInteraction("report_details:*")]
    public async Task ReportDetailsAsync(string reportId)
    {
        await DeferAsync(ephemeral: true);

        if (!Guid.TryParse(reportId, out var guid))
        {
            await FollowupAsync("Invalid report ID.", ephemeral: true);
            return;
        }

        var report = await _reportsService.GetReportByIdAsync(guid);

        if (report == null)
        {
            await FollowupAsync("Report not found.", ephemeral: true);
            return;
        }

        await FollowupAsync(embed: ScreenshotHandler.BuildReportEmbed(report).Build(), ephemeral: true);
    }

    private static ComponentBuilder BuildReportButtons(List<BattleReportResponse> reports)
    {
        var components = new ComponentBuilder();

        for (int i = 0; i < reports.Count; i++)
        {
            var report = reports[i];
            components.WithButton(
                label: $"{i + 1}",
                customId: $"report_details:{report.ReportId}",
                style: ButtonStyle.Secondary,
                row: i / 5);
        }

        return components;
    }
}
