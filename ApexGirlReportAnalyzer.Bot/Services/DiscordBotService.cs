using ApexGirlReportAnalyzer.Bot.Configuration;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace ApexGirlReportAnalyzer.Bot.Services;

public class DiscordBotService : BackgroundService
{
    private readonly DiscordSocketClient _client;

    private readonly DiscordLogService _discordLogService;

    private readonly IOptions<DiscordBotOptions> _options;

    public DiscordBotService(DiscordLogService discordLogService, IOptions<DiscordBotOptions> options)
    {
        _discordLogService = discordLogService;
        _options = options;
        _client = new DiscordSocketClient(new DiscordSocketConfig
        {
            LogLevel = LogSeverity.Info,
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
        });
        _client.Log += _discordLogService.LogAsync;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await _client.LoginAsync(TokenType.Bot, _options.Value.Token);
        await _client.StartAsync();

        await _client.SetActivityAsync(new Game("Analyzing Top Girl Reports"));

        await Task.Delay(Timeout.Infinite, stoppingToken);
    }

    public override async Task StopAsync(CancellationToken cancellationToken)
    {
        await _client.LogoutAsync();
        await _client.StopAsync();
        await base.StopAsync(cancellationToken);
    }
}
