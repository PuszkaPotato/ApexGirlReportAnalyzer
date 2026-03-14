namespace ApexGirlReportAnalyzer.Bot.Configuration;

/// <summary>
/// Configuration options for the API client.
/// </summary>
public class ApiOptions
{
    /// <summary>
    /// Base URL of the ApexGirl Report Analyzer API.
    /// </summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// API key for authentication. Set via user secrets.
    /// </summary>
    public string ApiKey { get; set; } = string.Empty;
}
