using ApexGirlReportAnalyzer.Bot.Configuration;
using Discord;
using Discord.Interactions;
using Microsoft.Extensions.Options;

namespace ApexGirlReportAnalyzer.Bot.Preconditions;

public class RequireDeveloperAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(IInteractionContext context, ICommandInfo commandInfo, IServiceProvider services)
    {
        var options = services.GetRequiredService<IOptions<DiscordBotOptions>>();

        if (context.User.Id != options.Value.DeveloperId)
            return Task.FromResult(PreconditionResult.FromError("This command is restricted to the developer."));

        return Task.FromResult(PreconditionResult.FromSuccess());
    }
}
