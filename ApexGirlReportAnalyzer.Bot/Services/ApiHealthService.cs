using ApexGirlReportAnalyzer.Bot.Configuration;
using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace ApexGirlReportAnalyzer.Bot.Services;

public class ApiHealthService : BackgroundService
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IOptions<ApiOptions> _apiOptions;
    private readonly DiscordSocketClient _discordClient;
    private readonly ILogger<ApiHealthService> _logger;

    public bool IsHealthy { get; private set; }

    public ApiHealthService(
        IHttpClientFactory httpClientFactory,
        IOptions<ApiOptions> apiOptions,
        DiscordSocketClient discordClient,
        ILogger<ApiHealthService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _apiOptions = apiOptions;
        _discordClient = discordClient;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            await CheckAndUpdateAsync();
            try { await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken); }
            catch (OperationCanceledException) { break; }
        }
    }

    private async Task CheckAndUpdateAsync()
    {
        var wasHealthy = IsHealthy;

        try
        {
            var httpClient = _httpClientFactory.CreateClient();
            var response = await httpClient.GetAsync($"{_apiOptions.Value.BaseUrl}/api/status/health");
            IsHealthy = response.IsSuccessStatusCode;
        }
        catch
        {
            IsHealthy = false;
        }

        if (IsHealthy != wasHealthy)
            _logger.LogInformation("API health changed: {Status}", IsHealthy ? "Healthy" : "Unhealthy");

        await UpdatePresenceAsync();
    }

    /// <summary>
    /// Updates Discord rich presence to reflect the current API health state.
    /// Safe to call at any time — no-ops if the client isn't connected yet.
    /// </summary>
    public async Task UpdatePresenceAsync()
    {
        if (_discordClient.ConnectionState != ConnectionState.Connected) return;

        if (IsHealthy)
        {
            await _discordClient.SetStatusAsync(UserStatus.Online);
            await _discordClient.SetActivityAsync(new Game("Analyzing Top Girl Reports"));
        }
        else
        {
            await _discordClient.SetStatusAsync(UserStatus.DoNotDisturb);
            await _discordClient.SetActivityAsync(new Game("Service unavailable — try again later"));
        }
    }
}
