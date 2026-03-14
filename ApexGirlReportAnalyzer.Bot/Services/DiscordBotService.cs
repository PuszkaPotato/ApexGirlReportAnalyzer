using ApexGirlReportAnalyzer.Bot.Configuration;
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

    public DiscordBotService(DiscordLogService discordLogService, IOptions<DiscordBotOptions> options, InteractionService interactionService)
    {
        _discordLogService = discordLogService;
        _options = options;
        _interactionService = interactionService;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        });
        _client.Log += _discordLogService.LogAsync;

        _client.Ready += OnReadyAsync;

        _client.InteractionCreated += async (interaction) =>
        {
            var context = new SocketInteractionContext(_client, interaction);
            await _interactionService.ExecuteCommandAsync(context, null);
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
        await _interactionService.AddModulesAsync(Assembly.GetEntryAssembly(), null);

#if DEBUG
    await _interactionService.RegisterCommandsToGuildAsync(_options.Value.TestGuildId);
#else
        await _interactionService.RegisterCommandsGloballyAsync();
#endif
    }
}
