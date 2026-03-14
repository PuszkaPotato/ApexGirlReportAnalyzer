using Discord;

namespace ApexGirlReportAnalyzer.Bot.Services;

public class DiscordLogService
{
    private readonly ILogger<DiscordLogService> _logger;
    public DiscordLogService(ILogger<DiscordLogService> logger)
    {
        _logger = logger;
    }

    public Task LogAsync(LogMessage message)
    {
        switch (message.Severity)
        {
            case LogSeverity.Info:
                _logger.LogInformation("{Source}: {Message}", message.Source, message.Message);
                break;
            case LogSeverity.Verbose:
                _logger.LogTrace("{Source}: {Message}", message.Source, message.Message);
                break;
            case LogSeverity.Debug:
                _logger.LogDebug("{Source}: {Message}", message.Source, message.Message);
                break;
            case LogSeverity.Warning:
                _logger.LogWarning("{Source}: {Message} {Exception}", message.Source, message.Message, message.Exception);
                break;
            case LogSeverity.Error:
                _logger.LogError("{Source}: {Message} {Exception}", message.Source, message.Message, message.Exception);
                break;
            case LogSeverity.Critical:
                _logger.LogCritical("{Source}: {Message} {Exception}", message.Source, message.Message, message.Exception);
                break;
        }

        return Task.CompletedTask;
    }
}
