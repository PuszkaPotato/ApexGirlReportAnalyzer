using ApexGirlReportAnalyzer.Bot.Configuration;
using ApexGirlReportAnalyzer.Bot.Handlers;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using System.Reflection;

namespace ApexGirlReportAnalyzer.Bot.Services;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;
    private readonly DiscordLogService _discordLogService;
    private readonly IOptions<DiscordBotOptions> _options;
    private readonly InteractionService _interactionService;
    private readonly IServiceProvider _serviceProvider;
    private readonly ScreenshotHandler _screenshotHandler;
    private readonly ApiHealthService _apiHealthService;
    private readonly ILogger<DiscordBotService> _logger;

    public DiscordBotService(
        DiscordSocketClient client,
        DiscordLogService discordLogService,
        IServiceProvider serviceProvider,
        IOptions<DiscordBotOptions> options,
        InteractionService interactionService,
        ScreenshotHandler screenshotHandler,
        ApiHealthService apiHealthService,
        ILogger<DiscordBotService> logger)
    {
        _client = client;
        _discordLogService = discordLogService;
        _serviceProvider = serviceProvider;
        _options = options;
        _interactionService = interactionService;
        _screenshotHandler = screenshotHandler;
        _apiHealthService = apiHealthService;
        _logger = logger;

        _client.Log += _discordLogService.LogAsync;
        _client.Ready += OnReadyAsync;
        _client.MessageReceived += message => Task.Run(() => _screenshotHandler.MessageReceived(message));

        _client.InteractionCreated += async (interaction) =>
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(context, _serviceProvider);
        };
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Discord bot starting...");

        await _client.LoginAsync(TokenType.Bot, _options.Value.Token);
        await _client.StartAsync();

        try
        {
            await Task.Delay(Timeout.Infinite, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Normal shutdown via stoppingToken — not an error
        }
    }

    private async Task OnReadyAsync()
    {
        _logger.LogInformation("Discord bot connected as {Username}", _client.CurrentUser.Username);
        await _apiHealthService.UpdatePresenceAsync();
        await RegisterCommands();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Discord bot stopping...");
        await _client.LogoutAsync();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
        _logger.LogInformation("Discord bot stopped.");
    }

    private async Task RegisterCommands()
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

#if DEBUG
        await _interactionService.RegisterCommandsToGuildAsync(_options.Value.TestGuildId);
        _logger.LogInformation("Slash commands registered to test guild {GuildId}.", _options.Value.TestGuildId);
#else
        await _interactionService.RegisterCommandsGloballyAsync();
        _logger.LogInformation("Slash commands registered globally.");
#endif
    }
}
