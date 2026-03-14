using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ApexGirlReportAnalyzer.Bot.Modules;

public class TierModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly TierService _tierService;

    public TierModule(TierService tierService)
    {
        _tierService = tierService;
    }

    [SlashCommand("assign-tier", "Assign a tier to a user.")]
    public async Task AssignTierAsync(
        [Summary("user", "The user to assign the tier to")] SocketGuildUser user,
        [Summary("tier", "The name of the tier to assign")] string tierName)
    {
        await DeferAsync(ephemeral: true);

        var success = await _tierService.AssignTierToUserAsync(user.Id.ToString(), tierName);

        if (!success)
        {
            await FollowupAsync($"Failed to assign tier **{tierName}** — tier not found or an error occurred.", ephemeral: true);
            return;
        }

        await FollowupAsync($"Tier **{tierName}** assigned to {user.Mention} successfully.", ephemeral: true);
    }
}
