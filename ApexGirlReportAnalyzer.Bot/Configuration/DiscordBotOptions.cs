namespace ApexGirlReportAnalyzer.Bot.Configuration;

/// <summary>
/// Configuration options for the Discord bot.
/// </summary>
public class DiscordBotOptions
{
    /// <summary>
    /// Display name of the bot.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Discord bot token. Set via user secrets.
    /// </summary>
    public string Token { get; set; } = string.Empty;
}
