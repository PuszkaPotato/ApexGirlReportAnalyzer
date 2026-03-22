using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;

namespace ApexGirlReportAnalyzer.Bot.Preconditions;

public class RequireApiHealthyAttribute : PreconditionAttribute
{
    public override Task<PreconditionResult> CheckRequirementsAsync(
        IInteractionContext context,
        ICommandInfo commandInfo,
        IServiceProvider services)
    {
        var health = services.GetRequiredService<ApiHealthService>();

        return health.IsHealthy
            ? Task.FromResult(PreconditionResult.FromSuccess())
            : Task.FromResult(PreconditionResult.FromError("The service is currently unavailable. Please try again later."));
    }
}
