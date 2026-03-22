using Discord;
using Discord.Interactions;

namespace ApexGirlReportAnalyzer.Bot.Modules;

public class HelpModule : InteractionModuleBase<SocketInteractionContext>
{
    [SlashCommand("help", "Learn how to use the ApexGirl Report Analyzer bot.")]
    public async Task HelpAsync()
    {
        var embed = new EmbedBuilder()
            .WithTitle("ApexGirl Report Analyzer")
            .WithDescription("I analyze battle report screenshots from Apex Girl and store the results for your group.")
            .WithColor(Color.Blue)
            .AddField("📸 How to Upload",
                "Attach a screenshot to the configured upload channel. I'll automatically analyze it and post the results.",
                inline: false)
            .AddField("📝 Extra Info (optional — single uploads only)",
                "Add player data to your message in this format:\n" +
                "`playerID team server enemyID team server`\n\n" +
                "Example: `123456 team1 942 654321 team2 939`\n" +
                "I'll show a confirmation before processing so you can verify it's correct.",
                inline: false)
            .AddField("🗂️ Multiple Screenshots",
                "Attach multiple screenshots at once — I'll process them all and list the results with buttons to view each report in detail.",
                inline: false)
            .AddField("📋 Commands",
                "`/reports` — browse battle reports with filters\n" +
                "`/export` — download reports as a CSV file\n" +
                "`/help` — this message",
                inline: false)
            .WithFooter("Extra info in the message is only supported for single-screenshot uploads.");

        await RespondAsync(embed: embed.Build(), ephemeral: true);
    }
}
