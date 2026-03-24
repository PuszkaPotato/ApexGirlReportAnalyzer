using ApexGirlReportAnalyzer.Bot.Handlers;
using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

namespace ApexGirlReportAnalyzer.Bot.Modules;

public class UploadConfirmModule : InteractionModuleBase<SocketInteractionContext>
{
    private readonly PendingUploadService _pendingUploadService;
    private readonly ApiClient _apiClient;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<UploadConfirmModule> _logger;

    public UploadConfirmModule(
        PendingUploadService pendingUploadService,
        ApiClient apiClient,
        IHttpClientFactory httpClientFactory,
        ILogger<UploadConfirmModule> logger)
    {
        _pendingUploadService = pendingUploadService;
        _apiClient = apiClient;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [ComponentInteraction("confirm_upload:*")]
    public async Task ConfirmUploadAsync(string correlationId)
    {
        await DeferAsync();

        var data = _pendingUploadService.GetAndRemove(correlationId);
        if (data == null)
        {
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = "This confirmation has expired or was already processed.";
                m.Embed = null;
                m.Components = new ComponentBuilder().Build();
            });
            return;
        }

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var imageResponse = await httpClient.GetAsync(data.ImageUrl);
            imageResponse.EnsureSuccessStatusCode();
            var imageStream = await imageResponse.Content.ReadAsStreamAsync();

            var uploadResult = await _apiClient.UploadScreenshotAsync(
                userId: data.UserId,
                discordServerId: data.DiscordServerId,
                discordChannelId: data.DiscordChannelId,
                discordMessageId: data.DiscordMessageId,
                imageStream: imageStream,
                fileName: data.FileName,
                playerInGameId: data.PlayerInGameId,
                enemyInGameId: data.EnemyInGameId,
                playerTeamRank: data.PlayerTeamRank,
                enemyTeamRank: data.EnemyTeamRank,
                playerServer: data.PlayerServer,
                enemyServer: data.EnemyServer,
                privacyScope: data.PrivacyScope);

            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = ScreenshotHandler.BuildReportEmbed(uploadResult).Build();
                m.Components = new ComponentBuilder().Build();
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing confirmed upload {CorrelationId}", correlationId);
            await ModifyOriginalResponseAsync(m =>
            {
                m.Content = string.Empty;
                m.Embed = new EmbedBuilder()
                    .WithTitle("Upload Failed")
                    .WithDescription("Something went wrong while processing your report. Please try again.")
                    .WithColor(Color.Red)
                    .Build();
                m.Components = new ComponentBuilder().Build();
            });
        }
    }

    [ComponentInteraction("cancel_upload:*")]
    public async Task CancelUploadAsync(string correlationId)
    {
        await DeferAsync();
        _pendingUploadService.GetAndRemove(correlationId);
        await ModifyOriginalResponseAsync(m =>
        {
            m.Content = "Upload cancelled. You can resubmit without the extra data.";
            m.Embed = null;
            m.Components = new ComponentBuilder().Build();
        });
    }
}
