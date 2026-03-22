using ApexGirlReportAnalyzer.Bot.Configuration;
using ApexGirlReportAnalyzer.Bot.Handlers;
using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Bot.Services;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<DiscordBotOptions>(builder.Configuration.GetSection("Bot"));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

builder.Services.AddSingleton(new DiscordSocketClient(new DiscordSocketConfig
{
    LogLevel = LogSeverity.Info,
    GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent
}));

builder.Services.AddSingleton(provider => new InteractionService(provider.GetRequiredService<DiscordSocketClient>()));

builder.Services.AddSingleton<DiscordLogService>();
builder.Services.AddSingleton<SetupService>();
builder.Services.AddSingleton<ReportsService>();
builder.Services.AddSingleton<TierService>();
builder.Services.AddSingleton<PendingUploadService>();
builder.Services.AddSingleton<ApiHealthService>();
builder.Services.AddHostedService(sp => sp.GetRequiredService<ApiHealthService>());

builder.Services.AddHttpClient<ScreenshotHandler>();

builder.Services.AddHttpClient<ApiClient>((serviceProvider, client) =>
{
    var config = serviceProvider.GetRequiredService<IConfiguration>();

    var baseUrl = config["Api:BaseUrl"]
        ?? throw new InvalidOperationException("Api:BaseUrl is not configured.");

    var apiKey = config["Api:ApiKey"]
        ?? throw new InvalidOperationException("Api:ApiKey is not configured.");

    client.BaseAddress = new Uri(baseUrl);
    client.DefaultRequestHeaders.Add("X-API-Key", apiKey);
});

builder.Services.AddHostedService<DiscordBotService>();

var host = builder.Build();
host.Run();
