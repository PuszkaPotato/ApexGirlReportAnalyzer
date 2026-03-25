using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ApexGirlReportAnalyzer.Bot.Preconditions;

public class RequireAllowedRoleAttribute : PreconditionAttribute
{
    public override async Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        if (context.Guild == null)
            return PreconditionResult.FromError("This command can only be used in a server.");

        var setupService = services.GetRequiredService<SetupService>();
        var config = await setupService.GetServerConfigAsync(context.Guild.Id.ToString());

        if (config == null)
            return PreconditionResult.FromError("This server hasn't been configured yet. An administrator needs to run `/setup init` first.");

        if (config.AllowedRoleId == null)
            return PreconditionResult.FromSuccess();

        if (context.User is not SocketGuildUser guildUser)
            return PreconditionResult.FromError("Could not verify your server roles.");

        if (!ulong.TryParse(config.AllowedRoleId, out var allowedRoleId))
            return PreconditionResult.FromSuccess();

        if (guildUser.Roles.Any(r => r.Id == allowedRoleId))
            return PreconditionResult.FromSuccess();

        return PreconditionResult.FromError("You don't have the required role to use this feature.");
    }
}
