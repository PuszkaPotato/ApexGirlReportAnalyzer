using ApexGirlReportAnalyzer.Models.Enums;
using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;

namespace ApexGirlReportAnalyzer.Bot.Modules;

[Group("setup", "Commands for setting up the bot in your server.")]
public class SetupModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly SetupService _setupService;

    public SetupModule(SetupService setupService)
    {
        _setupService = setupService;
    }

    [SlashCommand("init", "Configure the bot for your server.")]
    public async Task SetupAsync
        (
        [Summary("upload-channel", "The channel where the bot will listen for uploaded reports")] ITextChannel uploadChannel,
        [Summary("log-channel", "The channel where the bot logs will be visible")] ITextChannel? logChannel = null,
        [Summary("allowed-role", "The Role that is allowed to use the server quota on this server")] IRole? allowedRole = null,
        [Summary("privacy-scope", "The default privacy scope for reports uploaded on this server")] PrivacyScope privacyScope = PrivacyScope.Public
        )
    {
        var result = await _setupService.SetServerConfigAsync(Context.Guild.Id.ToString(), Context.Guild.OwnerId.ToString(), uploadChannel.Id.ToString(), logChannel?.Id.ToString(), allowedRole?.Id.ToString(), privacyScope);

        if (result == null)
            await RespondAsync("Something went wrong, please try again.");
        else
            await RespondAsync("Server configured successfully!");
    }
}
