using ApexGirlReportAnalyzer.Bot.Configuration;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace ApexGirlReportAnalyzer.Bot.Preconditions;

public class RequireAdminOrDeveloperAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<DiscordBotOptions>>();

        if (context.User.Id == options.Value.DeveloperId)
            return Task.FromResult(PreconditionResult.FromSuccess());

        if (context.User is SocketGuildUser guildUser && guildUser.GuildPermissions.Administrator)
            return Task.FromResult(PreconditionResult.FromSuccess());

        return Task.FromResult(PreconditionResult.FromError("This command requires Administrator permissions."));
    }
}
