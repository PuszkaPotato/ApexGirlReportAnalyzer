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

    public DiscordBotService(
        DiscordSocketClient client,
        DiscordLogService discordLogService,
        IServiceProvider serviceProvider,
        IOptions<DiscordBotOptions> options,
        InteractionService interactionService,
        ScreenshotHandler screenshotHandler)
    {
        _client = client;
        _discordLogService = discordLogService;
        _serviceProvider = serviceProvider;
        _options = options;
        _interactionService = interactionService;
        _screenshotHandler = screenshotHandler;

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
        await _client.LoginAsync(TokenType.Bot, _options.Value.Token);
        await _client.StartAsync();

        await _client.SetActivityAsync(new Game("Analyzing Top Girl Reports"));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    private async Task OnReadyAsync()
    {
        await RegisterCommands();
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }

    private async Task RegisterCommands()
    {
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), _serviceProvider);

#if DEBUG
        await _interactionService.RegisterCommandsToGuildAsync(_options.Value.TestGuildId);
#else
        await _interactionService.RegisterCommandsGloballyAsync();
#endif
    }
}
