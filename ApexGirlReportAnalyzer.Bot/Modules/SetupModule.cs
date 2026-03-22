using ApexGirlReportAnalyzer.Bot.Preconditions;
using ApexGirlReportAnalyzer.Bot.Services;
using ApexGirlReportAnalyzer.Models.Enums;
using Discord;
using Discord.Interactions;

namespace ApexGirlReportAnalyzer.Bot.Modules;

[RequireApiHealthy]
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

    [SlashCommand("view", "View the current bot configuration for this server.")]
    public async Task ViewAsync()
    {
        var config = await _setupService.GetServerConfigAsync(Context.Guild.Id.ToString());

        if (config == null)
        {
            await RespondAsync("This server hasn't been configured yet. Use `/setup init` to get started.");
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle("Server Configuration")
            .WithColor(Color.Blue)
            .AddField("Upload Channel", $"<#{config.UploadChannelId}>", inline: true)
            .AddField("Log Channel", config.LogChannelId != null ? $"<#{config.LogChannelId}>" : "Not set", inline: true)
            .AddField("Allowed Role", config.AllowedRoleId != null ? $"<@&{config.AllowedRoleId}>" : "All members", inline: true)
            .AddField("Default Privacy", config.DefaultReportPrivacy.ToString(), inline: true)
            .Build();

        await RespondAsync(embed: embed);
    }

    [SlashCommand("update", "Update the bot configuration for this server.")]
    public async Task UpdateAsync(
        [Summary("upload-channel", "The channel where the bot will listen for uploaded reports")] ITextChannel? uploadChannel = null,
        [Summary("log-channel", "The channel where the bot logs will be visible")] ITextChannel? logChannel = null,
        [Summary("allowed-role", "The role that is allowed to use the server quota")] IRole? allowedRole = null,
        [Summary("privacy-scope", "The default privacy scope for reports uploaded on this server")] PrivacyScope? privacyScope = null)
    {
        var result = await _setupService.UpdateServerConfigAsync(
            Context.Guild.Id.ToString(),
            Context.Guild.OwnerId.ToString(),
            uploadChannel?.Id.ToString(),
            logChannel?.Id.ToString(),
            allowedRole?.Id.ToString(),
            privacyScope);

        if (result == null)
            await RespondAsync("Could not update configuration. Has this server been set up with `/setup init` yet?");
        else
            await RespondAsync("Configuration updated successfully!");
    }
}
