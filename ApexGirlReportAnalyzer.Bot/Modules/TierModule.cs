using ApexGirlReportAnalyzer.Bot.Preconditions;
using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ApexGirlReportAnalyzer.Bot.Modules;

[RequireApiHealthy]
[RequireDeveloper]
public class TierModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TierService _tierService;

    public TierModule(TierService tierService)
    {
        _tierService = tierService;
    }

    [SlashCommand("tiers", "List all available tiers.")]
    public async Task ListTiersAsync()
    {
        await DeferAsync(ephemeral: true);

        var tiers = await _tierService.GetTiersAsync();

        if (tiers == null || tiers.Count == 0)
        {
            await FollowupAsync("No tiers found.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Available Tiers")
            .WithColor(Color.Blue);

        foreach (var tier in tiers)
            embed.AddField(tier.Name, $"ID: `{tier.Id}`", inline: false);

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }

    [SlashCommand("assign-tier", "Assign a tier to a user.")]
    public async Task AssignTierToUserAsync(
        [Summary("user", "The user to assign the tier to")] SocketGuildUser user,
        [Summary("tier", "The name of the tier to assign")] string tierName)
    {
        await DeferAsync(ephemeral: true);

        var (success, userNotRegistered) = await _tierService.AssignTierToUserAsync(user.Id.ToString(), tierName);

        if (!success)
        {
            var errorMessage = userNotRegistered
                ? $"{user.Mention} has not registered yet — they need to upload their first report before a tier can be assigned."
                : $"Failed to assign tier **{tierName}** — tier not found or an error occurred.";
            await FollowupAsync(errorMessage, ephemeral: true);
            return;
        }

        await FollowupAsync($"Tier **{tierName}** assigned to {user.Mention} successfully.", ephemeral: true);
    }

    [SlashCommand("assign-tier-server", "Assign a tier to a server by its Discord ID.")]
    public async Task AssignTierToServerAsync(
        [Summary("server-id", "The Discord server ID to assign the tier to")] string serverId,
        [Summary("tier", "The name of the tier to assign")] string tierName)
    {
        await DeferAsync(ephemeral: true);

        var success = await _tierService.AssignTierToServerAsync(serverId, tierName);

        if (!success)
        {
            await FollowupAsync($"Failed to assign tier **{tierName}** — server/tier not found or an error occurred.", ephemeral: true);
            return;
        }

        await FollowupAsync($"Tier **{tierName}** assigned to server `{serverId}` successfully.", ephemeral: true);
    }

    [SlashCommand("quota", "Check a user's remaining upload quota.")]
    public async Task GetQuotaAsync(
        [Summary("user", "The user to check quota for")] SocketGuildUser user)
    {
        await DeferAsync(ephemeral: true);

        var quota = await _tierService.GetUserQuotaAsync(user.Id.ToString());

        if (quota == null)
        {
            await FollowupAsync($"Failed to retrieve quota for {user.Mention}.", ephemeral: true);
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Quota — {user.DisplayName}")
            .WithColor(Color.Blue)
            .AddField("Tier", quota.TierName, inline: false)
            .AddField("Daily Remaining", quota.DailyRemaining.ToString(), inline: true)
            .AddField("Monthly Remaining", quota.MonthlyRemaining.ToString(), inline: true);

        if (quota.ServerTierName != null)
            embed
                .AddField("\u200b", "\u200b")
                .AddField("Server Tier", quota.ServerTierName, inline: false)
                .AddField("Server Daily Remaining", quota.ServerDailyRemaining?.ToString() ?? "N/A", inline: true)
                .AddField("Server Monthly Remaining", quota.ServerMonthlyRemaining?.ToString() ?? "N/A", inline: true);

        await FollowupAsync(embed: embed.Build(), ephemeral: true);
    }
}
