using ApexGirlReportAnalyzer.Bot.Configuration;
using ApexGirlReportAnalyzer.Bot.Http;
using ApexGirlReportAnalyzer.Bot.Services;
using Discord.Interactions;

var builder = Host.CreateApplicationBuilder(args);

builder.Services.Configure<DiscordBotOptions>(builder.Configuration.GetSection("Bot"));
builder.Services.Configure<ApiOptions>(builder.Configuration.GetSection("Api"));

builder.Services.AddSingleton<DiscordLogService>();
builder.Services.AddSingleton<InteractionService>();
builder.Services.AddSingleton<SetupService>();
builder.Services.AddSingleton<ReportsService>();

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
