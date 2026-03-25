using System.Text.RegularExpressions;
using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Bot.Services;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;
using Discord;
using Discord.WebSocket;

namespace ApexGirlReportAnalyzer.Bot.Handlers;

public record ParsedUploadMetadata(
    string PlayerInGameId,
    int PlayerTeamRank,
    int PlayerServer,
    string EnemyInGameId,
    int EnemyTeamRank,
    int EnemyServer);

public class ScreenshotHandler
{
    private readonly ApiClient _apiClient;
    private readonly ILogger<ScreenshotHandler> _logger;
    private readonly HttpClient _httpClient;
    private readonly SetupService _setupService;
    private readonly PendingUploadService _pendingUploadService;
    private readonly ApiHealthService _apiHealthService;

    private static readonly Regex MetadataRegex = new(
        @"(\d{5,6})\s+(team[1-6]|b[1-6]|[1-6])\s+(\d{3,4})\s+(\d{5,6})\s+(team[1-6]|b[1-6]|[1-6])\s+(\d{3,4})",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public ScreenshotHandler(
        ApiClient apiClient,
        ILogger<ScreenshotHandler> logger,
        HttpClient httpClient,
        SetupService setupService,
        PendingUploadService pendingUploadService,
        ApiHealthService apiHealthService)
    {
        _apiClient = apiClient;
        _logger = logger;
        _httpClient = httpClient;
        _setupService = setupService;
        _pendingUploadService = pendingUploadService;
        _apiHealthService = apiHealthService;
    }

    public async Task MessageReceived(SocketMessage message)
    {
        if (message.Attachments.Count == 0) return;
        if (message.Author.IsBot) return;

        var guild = ((SocketGuildChannel)message.Channel).Guild;
        var server = await _setupService.GetServerConfigAsync(guild.Id.ToString());

        if (server == null)
        {
            _logger.LogWarning("Received message in channel {ChannelId} but no server config found for guild {GuildId}", message.Channel.Id, guild.Id);
            return;
        }

        if (message.Channel.Id.ToString() != server.UploadChannelId) return;

        if (server.AllowedRoleId != null &&
            message.Author is SocketGuildUser guildUser &&
            ulong.TryParse(server.AllowedRoleId, out var allowedRoleId) &&
            !guildUser.Roles.Any(r => r.Id == allowedRoleId))
        {
            if (message is IUserMessage restrictedMsg)
                await restrictedMsg.ReplyAsync("You don't have the required role to upload reports on this server.");
            return;
        }

        var validImageTypes = new[] { "image/jpeg", "image/png" };
        var imageAttachments = message.Attachments
            .Where(a => a.ContentType is not null && validImageTypes.Any(t => a.ContentType.StartsWith(t)))
            .ToList();

        if (imageAttachments.Count == 0) return;

        var userMessage = message as IUserMessage;
        if (userMessage == null) return;

        if (!_apiHealthService.IsHealthy)
        {
            await userMessage.ReplyAsync("The service is currently unavailable. Please try again later.");
            return;
        }

        _logger.LogInformation("Received {Count} image attachment(s) in channel {ChannelId} from user {UserId}",
            imageAttachments.Count, message.Channel.Id, message.Author.Id);

        // Multi-image batch path
        if (imageAttachments.Count > 1)
        {
            var hasMetadata = ParseMetadata(message.Content) != null;
            string? processingText = hasMetadata
                ? $"Processing {imageAttachments.Count} reports... (note: extra info in the message is not supported for batch uploads and was ignored)"
                : $"Processing {imageAttachments.Count} reports...";

            var processingMessage = await userMessage.ReplyAsync(processingText);
            await ProcessBatchAsync(processingMessage, userMessage, imageAttachments, guild.Id.ToString(), server.DefaultReportPrivacy);
            return;
        }

        // Single image path
        var attachment = imageAttachments[0];
        var metadata = ParseMetadata(message.Content);

        if (metadata != null)
        {
            var userId = await GetUserId(message.Author.Id.ToString());
            var correlationId = _pendingUploadService.Add(new PendingUploadData(
                UserId: userId,
                DiscordUserId: message.Author.Id.ToString(),
                DiscordServerId: guild.Id.ToString(),
                DiscordChannelId: message.Channel.Id.ToString(),
                DiscordMessageId: message.Id.ToString(),
                ImageUrl: attachment.Url,
                FileName: attachment.Filename,
                PlayerInGameId: metadata.PlayerInGameId,
                PlayerTeamRank: metadata.PlayerTeamRank,
                PlayerServer: metadata.PlayerServer,
                EnemyInGameId: metadata.EnemyInGameId,
                EnemyTeamRank: metadata.EnemyTeamRank,
                EnemyServer: metadata.EnemyServer,
                PrivacyScope: server.DefaultReportPrivacy));

            var components = new ComponentBuilder()
                .WithButton("Confirm", $"confirm_upload:{correlationId}", ButtonStyle.Success)
                .WithButton("Cancel", $"cancel_upload:{correlationId}", ButtonStyle.Danger)
                .Build();

            await userMessage.ReplyAsync(embed: BuildConfirmEmbed(metadata).Build(), components: components);
            return;
        }

        var processingMsg = await userMessage.ReplyAsync("Your report is being processed...");

        try
        {
            var uploadResult = await _apiClient.UploadScreenshotAsync(
                userId: await GetUserId(message.Author.Id.ToString()),
                discordUserId: message.Author.Id.ToString(),
                discordServerId: guild.Id.ToString(),
                discordChannelId: message.Channel.Id.ToString(),
                discordMessageId: message.Id.ToString(),
                imageStream: await DownloadImageAsync(attachment.Url),
                fileName: attachment.Filename,
                privacyScope: server.DefaultReportPrivacy);

            _logger.LogWarning(
                "UploadResult: Success={Success}, BattleDataNull={BattleDataNull}, IsDuplicate={IsDuplicate}",
                uploadResult?.Success,
                uploadResult?.BattleData == null,
                uploadResult?.IsDuplicate);

            await processingMsg.ModifyAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = BuildReportEmbed(uploadResult).Build();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error has occured while processing the upload");
            await processingMsg.ModifyAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = BuildReportEmbed(null).Build();
            });
        }
    }

    #region Embed Builders

    public static EmbedBuilder BuildReportEmbed(UploadResponse? result)
    {
        if (result == null)
            return new EmbedBuilder()
                .WithTitle("Upload Failed")
                .WithDescription("Could not reach the analysis service. Please try again.")
                .WithColor(Color.Red);

        if (result.Success && result.BattleData == null)
            return new EmbedBuilder()
                .WithTitle("Upload Failed")
                .WithDescription("BattleData is null, this is an unexpected error, please report it to the developer")
                .WithColor(Color.Red);

        if (!result.Success)
            return new EmbedBuilder()
                .WithTitle("Upload Failed")
                .WithDescription("An unexpected error occurred.")
                .WithColor(Color.Red);

        var color = result.IsDuplicate ? Color.Orange : Color.Default;
        var titlePrefix = result.IsDuplicate ? "Duplicate — " : "Battle Report — ";

        return BuildReportEmbed(result.BattleData!, titlePrefix, color)
            .WithFooter(result.RemainingQuota != null
                ? $"Quota remaining — Daily: {result.RemainingQuota.DailyRemaining} | Monthly: {result.RemainingQuota.MonthlyRemaining}"
                : string.Empty);
    }

    public static EmbedBuilder BuildReportEmbed(BattleReportResponse data, string titlePrefix = "Battle Report — ", Color color = default)
    {
        var player = data.Player;
        var enemy = data.Enemy;

        var playerName = player.Username ?? player.InGamePlayerId ?? "Unknown";
        var enemyName = enemy.Username ?? enemy.InGamePlayerId ?? "Unknown";
        var playerTag = player.GroupTag != null ? $" [{player.GroupTag}]" : "";
        var enemyTag = enemy.GroupTag != null ? $" [{enemy.GroupTag}]" : "";

        if (color == default)
            color = player.LossCount <= enemy.LossCount ? Color.Green : Color.Red;

        return new EmbedBuilder()
            .WithTitle($"{titlePrefix}{data.BattleType} | {data.BattleDate:yyyy-MM-dd}")
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
                $"Active: {player.ActiveSkill / 100}%\nBasic Attack: {player.BasicAttackBonus / 100}%\nSkill Bonus: {player.SkillBonus / 100}%\nSkill Reduction: {player.SkillReduction / 100}%\nExtra Damage: {player.ExtraDamage}",
                inline: true)
            .AddField("Enemy Skills",
                $"Active: {enemy.ActiveSkill / 100}%\nBasic Attack: {enemy.BasicAttackBonus / 100}%\nSkill Bonus: {enemy.SkillBonus / 100}%\nSkill Reduction: {enemy.SkillReduction / 100}%\nExtra Damage: {enemy.ExtraDamage}",
                inline: true);
    }

    private static EmbedBuilder BuildConfirmEmbed(ParsedUploadMetadata metadata)
    {
        return new EmbedBuilder()
            .WithTitle("Is this extra information correct?")
            .WithColor(Color.Blue)
            .AddField("Player", $"In-Game ID: {metadata.PlayerInGameId}\nTeam Rank: {metadata.PlayerTeamRank}\nServer: {metadata.PlayerServer}", inline: true)
            .AddField("Enemy", $"In-Game ID: {metadata.EnemyInGameId}\nTeam Rank: {metadata.EnemyTeamRank}\nServer: {metadata.EnemyServer}", inline: true)
            .WithFooter("Confirm to process your report, or cancel to upload without this data.");
    }

    #endregion

    #region Private Helper Methods

    private async Task ProcessBatchAsync(
        IUserMessage processingMessage,
        IUserMessage originalMessage,
        List<Discord.Attachment> attachments,
        string guildId,
        PrivacyScope privacyScope)
    {
        Guid userId;
        try
        {
            userId = await GetUserId(originalMessage.Author.Id.ToString());
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to resolve user for batch upload");
            await processingMessage.ModifyAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = new EmbedBuilder()
                    .WithTitle("Batch Upload Failed")
                    .WithDescription("Could not resolve your user account. Please try again.")
                    .WithColor(Color.Red)
                    .Build();
            });
            return;
        }

        var results = new List<UploadResponse?>();

        foreach (var attachment in attachments)
        {
            try
            {
                var result = await _apiClient.UploadScreenshotAsync(
                    userId: userId,
                    discordUserId: originalMessage.Author.Id.ToString(),
                    discordServerId: guildId,
                    discordChannelId: originalMessage.Channel.Id.ToString(),
                    discordMessageId: originalMessage.Id.ToString(),
                    imageStream: await DownloadImageAsync(attachment.Url),
                    fileName: attachment.Filename,
                    privacyScope: privacyScope);

                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image {FileName} in batch", attachment.Filename);
                results.Add(null);
            }
        }

        var successes = results.Where(r => r?.Success == true && r.BattleData != null).ToList();
        var failCount = results.Count - successes.Count;

        if (successes.Count == 0)
        {
            await processingMessage.ModifyAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = new EmbedBuilder()
                    .WithTitle("Batch Upload Failed")
                    .WithDescription("All uploads failed to process. Please try again.")
                    .WithColor(Color.Red)
                    .Build();
            });
            return;
        }

        var footer = failCount > 0
            ? $"{failCount} upload(s) failed | Click a button to view full details"
            : "Click a button to view full details";

        var embed = new EmbedBuilder()
            .WithTitle($"Batch Upload — {successes.Count} report(s) processed")
            .WithColor(Color.Green)
            .WithFooter(footer);

        for (int i = 0; i < successes.Count; i++)
        {
            var report = successes[i]!.BattleData!;
            var playerName = report.Player.Username ?? report.Player.InGamePlayerId ?? "Unknown";
            var enemyName = report.Enemy.Username ?? report.Enemy.InGamePlayerId ?? "Unknown";
            embed.AddField(
                $"{i + 1}. {playerName} vs {enemyName}",
                $"**Type:** {report.BattleType} | **Date:** {report.BattleDate:yyyy-MM-dd}",
                inline: false);
        }

        var components = new ComponentBuilder();
        for (int i = 0; i < successes.Count; i++)
        {
            components.WithButton(
                label: $"{i + 1}",
                customId: $"report_details:{successes[i]!.BattleData!.ReportId}",
                style: ButtonStyle.Secondary,
                row: i / 5);
        }

        await processingMessage.ModifyAsync(m =>
        {
            m.Content = string.Empty;
            m.Embed = embed.Build();
            m.Components = components.Build();
        });
    }

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

    private static ParsedUploadMetadata? ParseMetadata(string? content)
    {
        if (string.IsNullOrWhiteSpace(content)) return null;

        var match = MetadataRegex.Match(content);
        if (!match.Success) return null;

        return new ParsedUploadMetadata(
            PlayerInGameId: match.Groups[1].Value,
            PlayerTeamRank: int.Parse(Regex.Replace(match.Groups[2].Value, @"[^\d]", "")),
            PlayerServer: int.Parse(match.Groups[3].Value),
            EnemyInGameId: match.Groups[4].Value,
            EnemyTeamRank: int.Parse(Regex.Replace(match.Groups[5].Value, @"[^\d]", "")),
            EnemyServer: int.Parse(match.Groups[6].Value));
    }

    #endregion
}
