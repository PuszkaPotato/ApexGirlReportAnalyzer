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

    /// <summary>
    /// Test server discord guild ID for registering commands during development.
    /// </summary>
    public ulong TestGuildId { get; set; } = ulong.MaxValue;

    /// <summary>
    /// Discord user ID of the developer. Used to restrict access to developer-only commands.
    /// </summary>
    public ulong DeveloperId { get; set; }
}
