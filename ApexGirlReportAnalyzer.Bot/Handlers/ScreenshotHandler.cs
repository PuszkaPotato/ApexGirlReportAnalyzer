using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Bot.Services;
using ApexGirlReportAnalyzer.Models.DTOs;
using Discord;
using Discord.WebSocket;

namespace ApexGirlReportAnalyzer.Bot.Handlers;

public class ScreenshotHandler
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<ScreenshotHandler> _logger;
    private readonly HttpClient _httpClient;
    private readonly SetupService _setupService;

    public ScreenshotHandler(ApiClient apiClient, ILogger<ScreenshotHandler> logger, HttpClient httpClient, SetupService setupService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _httpClient = httpClient;
        _setupService = setupService;
    }

    public async Task MessageReceived(SocketMessage message)
    {
        if (message.Attachments.Count == 0) return;
        if (message.Author.IsBot) return;

        var server = await _setupService.GetServerConfigAsync(((SocketGuildChannel)message.Channel).Guild.Id.ToString());

        if (server == null)
        {
            _logger.LogWarning("Received message in channel {ChannelId} but no server config found for guild {GuildId}", message.Channel.Id, ((SocketGuildChannel)message.Channel).Guild.Id);
            return;
        }

        if (message.Channel.Id.ToString() != server.UploadChannelId) return;

        var attachment = message.Attachments.FirstOrDefault(a => a.ContentType != null && a.ContentType.StartsWith("image/"));
        if (attachment == null) return;

        _logger.LogInformation("Received image attachment in channel {ChannelId} from user {UserId}", message.Channel.Id, message.Author.Id);

        var userMessage = message as IUserMessage;
        if (userMessage == null)
            return;

        try
        {
            var uploadResult = await _apiClient.UploadScreenshotAsync(
                userId: await GetUserId(message.Author.Id.ToString()),
                discordServerId: ((SocketGuildChannel)message.Channel).Guild.Id.ToString(),
                discordChannelId: message.Channel.Id.ToString(),
                discordMessageId: message.Id.ToString(),
                imageStream: await DownloadImageAsync(attachment.Url),
                fileName: attachment.Filename);

            await userMessage.ReplyAsync(embed: BuildReportEmbed(uploadResult).Build());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occured while processing the upload");
            await userMessage.ReplyAsync(embed: BuildReportEmbed(null).Build());
        }
    }

    #region Private Helper Methods

    private async Task<Guid> GetUserId(string discordUserId)
    {
        var user = await _apiClient.GetOrCreateUserAsync(discordUserId);
        if (user == null)
            throw new Exception($"Failed to get or create user for Discord ID {discordUserId}");

        return user.Id;
    }

    private async Task<Stream> DownloadImageAsync(string imageUrl)
    {
        try
        {
            var response = await _httpClient.GetAsync(imageUrl);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsStreamAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to download image from URL {ImageUrl}", imageUrl);
            throw;
        }
    }

    private static EmbedBuilder BuildReportEmbed(UploadResponse? result)
    {
        if (result == null)
            return new EmbedBuilder()
                .WithTitle("Upload Failed")
                .WithDescription("Could not reach the analysis service. Please try again.")
                .WithColor(Color.Red);

        if (result.IsDuplicate)
            return new EmbedBuilder()
                .WithTitle("Duplicate Screenshot")
                .WithDescription("This screenshot has already been processed.")
                .WithColor(Color.Orange);

        if (!result.Success || result.BattleData == null)
            return new EmbedBuilder()
                .WithTitle("Upload Failed")
                .WithDescription(result.ErrorMessage ?? "An unexpected error occurred.")
                .WithColor(Color.Red);

        var data = result.BattleData;
        var player = data.Player;
        var enemy = data.Enemy;

        var playerName = player.Username ?? player.InGamePlayerId ?? "Unknown";
        var enemyName = enemy.Username ?? enemy.InGamePlayerId ?? "Unknown";
        var playerTag = player.GroupTag != null ? $" [{player.GroupTag}]" : "";
        var enemyTag = enemy.GroupTag != null ? $" [{enemy.GroupTag}]" : "";

        var color = player.LossCount <= enemy.LossCount ? Color.Green : Color.Red;

        return new EmbedBuilder()
            .WithTitle($"Battle Report — {data.BattleType} | {data.BattleDate:yyyy-MM-dd}")
            .WithColor(color)
            .AddField("Player", $"{playerName}{playerTag} | Lv.{player.Level}", inline: true)
            .AddField("Enemy", $"{enemyName}{enemyTag} | Lv.{enemy.Level}", inline: true)
            .AddField("\u200b", "\u200b")
            .AddField("Player Troops",
                $"Fans: {player.FanCount}\nLosses: {player.LossCount}\nInjured: {player.InjuredCount}\nRemaining: {player.RemainingCount ?? 0}",
                inline: true)
            .AddField("Enemy Troops",
                $"Fans: {enemy.FanCount}\nLosses: {enemy.LossCount}\nInjured: {enemy.InjuredCount}\nRemaining: {enemy.RemainingCount ?? 0}",
                inline: true)
            .AddField("\u200b", "\u200b")
            .AddField("Player Attributes",
                $"Sing: {player.Sing}\nDance: {player.Dance}",
                inline: true)
            .AddField("Enemy Attributes",
                $"Sing: {enemy.Sing}\nDance: {enemy.Dance}",
                inline: true)
            .AddField("\u200b", "\u200b")
            .AddField("Player Skills",
                $"Active: {player.ActiveSkill / 100}%\nBasic Attack: {player.BasicAttackBonus / 100}%\nSkill Bonus: {player.SkillBonus / 100}%\nSkill Reduction: {player.SkillReduction / 100}%",
                inline: true)
            .AddField("Enemy Skills",
                $"Active: {enemy.ActiveSkill / 100}%\nBasic Attack: {enemy.BasicAttackBonus / 100}%\nSkill Bonus: {enemy.SkillBonus / 100}%\nSkill Reduction: {enemy.SkillReduction / 100}%",
                inline: true)
            .WithFooter(result.RemainingQuota != null
                ? $"Quota remaining — Daily: {result.RemainingQuota.DailyRemaining} | Monthly: {result.RemainingQuota.MonthlyRemaining}"
                : string.Empty);
    }

    #endregion
}
