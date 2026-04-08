using System.Text.RegularExpressions;
using ApexGirlReportAnalyzer.Bot.Handlers;
using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Bot.Preconditions;
using ApexGirlReportAnalyzer.Bot.Services;
using ApexGirlReportAnalyzer.Models.DTOs;
using Discord;
using Discord.Interactions;

namespace ApexGirlReportAnalyzer.Bot.Modules;

public class RecheckModule : InteractionModuleBase<SocketInteractionContext>
{
    private static readonly Regex MessageLinkRegex = new(
        @"channels/\d+/(\d+)/(\d+)",
        RegexOptions.Compiled);

    private readonly ApiClient _apiClient;
    private readonly SetupService _setupService;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RecheckModule> _logger;

    public RecheckModule(
        ApiClient apiClient,
        SetupService setupService,
        HttpClient httpClient,
        ILogger<RecheckModule> logger)
    {
        _apiClient = apiClient;
        _setupService = setupService;
        _httpClient = httpClient;
        _logger = logger;
    }

    [SlashCommand("recheck", "Manually reprocess a message's images as battle reports")]
    [RequireAllowedRole]
    public async Task Recheck(
        [Summary("message_link", "Right-click a message → Copy Message Link, or paste just the message ID")] string messageInput)
    {
        await DeferAsync();

        // Accept full message link (https://discord.com/channels/{guild}/{channel}/{message})
        // or a bare message ID (searches current channel)
        ulong channelId;
        ulong messageId;

        var match = MessageLinkRegex.Match(messageInput.Trim());
        if (match.Success)
        {
            channelId = ulong.Parse(match.Groups[1].Value);
            messageId = ulong.Parse(match.Groups[2].Value);
        }
        else if (ulong.TryParse(messageInput.Trim(), out messageId))
        {
            channelId = Context.Channel.Id;
        }
        else
        {
            await FollowupAsync("Invalid input. Paste a message link (right-click → Copy Message Link) or a message ID.", ephemeral: true);
            return;
        }

        var channel = await Context.Client.GetChannelAsync(channelId) as ITextChannel;
        if (channel == null)
        {
            await FollowupAsync("Could not access that channel. Make sure I have permission to read it.", ephemeral: true);
            return;
        }

        var message = await channel.GetMessageAsync(messageId);
        if (message == null)
        {
            await FollowupAsync("Message not found.", ephemeral: true);
            return;
        }

        var imageAttachments = message.Attachments
            .Where(a =>
                a.Filename.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ||
                a.Filename.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                a.Filename.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (imageAttachments.Count == 0)
        {
            await FollowupAsync("No image attachments found in that message.", ephemeral: true);
            return;
        }

        var server = await _setupService.GetServerConfigAsync(Context.Guild.Id.ToString());
        if (server == null)
        {
            await FollowupAsync("Server not configured. Run /setup init first.", ephemeral: true);
            return;
        }

        var userObj = await _apiClient.GetOrCreateUserAsync(Context.User.Id.ToString());
        if (userObj == null)
        {
            await FollowupAsync("Could not resolve your user account. Please try again.", ephemeral: true);
            return;
        }

        await FollowupAsync($"Processing {imageAttachments.Count} image(s)...");

        var results = new List<UploadResponse?>();
        foreach (var attachment in imageAttachments)
        {
            try
            {
                var imageResponse = await _httpClient.GetAsync(attachment.Url);
                imageResponse.EnsureSuccessStatusCode();
                var stream = await imageResponse.Content.ReadAsStreamAsync();
                var result = await _apiClient.UploadScreenshotAsync(
                    imageStream: stream,
                    fileName: attachment.Filename,
                    userId: userObj.Id,
                    discordUserId: Context.User.Id.ToString(),
                    discordServerId: Context.Guild.Id.ToString(),
                    discordChannelId: channel.Id.ToString(),
                    discordMessageId: messageId.ToString(),
                    privacyScope: server.DefaultReportPrivacy);
                results.Add(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing image {FileName} during recheck", attachment.Filename);
                results.Add(null);
            }
        }

        if (results.Count == 1)
        {
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = ScreenshotHandler.BuildReportEmbed(results[0]).Build();
            });
            return;
        }

        // Multiple images — summary with detail buttons (mirrors batch upload behaviour)
        var successes = results.Where(r => r?.Success == true && r.BattleData != null).ToList();
        var failCount = results.Count - successes.Count;

        if (successes.Count == 0)
        {
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = new EmbedBuilder()
                    .WithTitle("Recheck Failed")
                    .WithDescription("No images could be processed.")
                    .WithColor(Color.Red)
                    .Build();
            });
            return;
        }

        var embed = new EmbedBuilder()
            .WithTitle($"Recheck — {successes.Count} report(s) processed")
            .WithColor(Color.Green)
            .WithFooter(failCount > 0
                ? $"{failCount} image(s) failed | Click a button to view full details"
                : "Click a button to view full details");

        for (int i = 0; i < successes.Count; i++)
        {
            var report = successes[i]!.BattleData!;
            var playerName = report.Player.Username ?? report.Player.InGamePlayerId ?? "Unknown";
            var enemyName = report.Enemy.Username ?? report.Enemy.InGamePlayerId ?? "Unknown";
            embed.AddField(
                $"{i + 1}. {playerName} vs {enemyName}",
                $"**Type:** {report.BattleType} | **Date:** {report.BattleDate:yyyy-MM-dd}");
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

        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = string.Empty;
            m.Embed = embed.Build();
            m.Components = components.Build();
        });
    }
}
