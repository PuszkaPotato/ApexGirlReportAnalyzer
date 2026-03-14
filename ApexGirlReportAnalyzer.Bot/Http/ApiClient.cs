using System.Net.Http.Json;
using System.Text.Json;
using ApexGirlReportAnalyzer.Models.DTOs;

namespace ApexGirlReportAnalyzer.Bot.Http;

/// <summary>
/// Typed HTTP client for communicating with the ApexGirl Report Analyzer API.
/// </summary>
public class ApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ApiClient> _logger;

    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public ApiClient(HttpClient httpClient, ILogger<ApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Gets an existing user or creates a new one by their Discord ID.
    /// </summary>
    public async Task<UserResponse?> GetOrCreateUserAsync(string discordId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting or creating user for Discord ID {DiscordId}", discordId);

        var response = await _httpClient.PostAsync(
            $"api/user/get-or-create?discordId={Uri.EscapeDataString(discordId)}",
            null,
            cancellationToken);

        return await DeserializeResponseAsync<UserResponse>(response, nameof(GetOrCreateUserAsync));
    }

    /// <summary>
    /// Gets the remaining upload quota for a user.
    /// </summary>
    public async Task<QuotaInfo?> GetUserQuotaAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting quota for user {UserId}", userId);

        var response = await _httpClient.GetAsync(
            $"api/user/quota/{userId}",
            cancellationToken);

        return await DeserializeResponseAsync<QuotaInfo>(response, nameof(GetUserQuotaAsync));
    }

    /// <summary>
    /// Uploads a battle report screenshot for analysis.
    /// </summary>
    public async Task<UploadResponse?> UploadScreenshotAsync(
        Stream imageStream,
        string fileName,
        Guid userId,
        string discordServerId,
        string? discordChannelId = null,
        string? discordMessageId = null,
        CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Uploading screenshot for user {UserId} in server {ServerId}", userId, discordServerId);

        using var content = new MultipartFormDataContent();

        var imageContent = new StreamContent(imageStream);
        imageContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(
            fileName.EndsWith(".png", StringComparison.OrdinalIgnoreCase) ? "image/png" : "image/jpeg");

        content.Add(imageContent, "image", fileName);
        content.Add(new StringContent(userId.ToString()), "userId");
        content.Add(new StringContent(discordServerId), "discordServerId");

        if (discordChannelId != null)
            content.Add(new StringContent(discordChannelId), "discordChannelId");

        if (discordMessageId != null)
            content.Add(new StringContent(discordMessageId), "discordMessageId");

        var response = await _httpClient.PostAsync("api/upload", content, cancellationToken);

        return await DeserializeResponseAsync<UploadResponse>(response, nameof(UploadScreenshotAsync));
    }

    /// <summary>
    /// Gets the configuration for a Discord server, or null if not yet configured.
    /// </summary>
    public async Task<DiscordServerConfigResponse?> GetServerConfigAsync(string discordServerId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting config for Discord server {ServerId}", discordServerId);

        var response = await _httpClient.GetAsync(
            $"api/discordserver/{Uri.EscapeDataString(discordServerId)}",
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NotFound)
            return null;

        return await DeserializeResponseAsync<DiscordServerConfigResponse>(response, nameof(GetServerConfigAsync));
    }

    /// <summary>
    /// Sets or updates the configuration for a Discord server.
    /// </summary>
    public async Task<DiscordServerConfigResponse?> SetServerConfigAsync(DiscordServerConfigRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Setting config for Discord server {ServerId}", request.DiscordServerId);

        var response = await _httpClient.PostAsJsonAsync("api/discordserver/config", request, cancellationToken);

        return await DeserializeResponseAsync<DiscordServerConfigResponse>(response, nameof(SetServerConfigAsync));
    }

    /// <summary>
    /// Queries battle reports with optional filters and pagination.
    /// </summary>
    public async Task<BattleReportListResponse?> GetBattleReportsAsync(
        Guid? userId = null,
        string? participant = null,
        int limit = 10,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        var query = BuildReportsQueryString(userId, participant, limit, offset);
        _logger.LogDebug("Querying battle reports: {Query}", query);

        var response = await _httpClient.GetAsync($"api/battlereport?{query}", cancellationToken);

        return await DeserializeResponseAsync<BattleReportListResponse>(response, nameof(GetBattleReportsAsync));
    }

    /// <summary>
    /// Gets all tiers.
    /// </summary>
    public async Task<List<TierResponse>?> GetTiersAsync(CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting all tiers");

        var response = await _httpClient.GetAsync("api/tiers", cancellationToken);

        return await DeserializeResponseAsync<List<TierResponse>>(response, nameof(GetTiersAsync));
    }

    /// <summary>
    /// Assigns a tier to a user by their Discord ID.
    /// </summary>
    public async Task<bool> AssignTierToUserAsync(string discordUserId, Guid tierId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Assigning tier {TierId} to user {DiscordUserId}", tierId, discordUserId);

        var response = await _httpClient.PutAsync(
            $"api/tiers/{tierId}/assign-user/{Uri.EscapeDataString(discordUserId)}",
            null,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    #region Private Helper Methods

    private async Task<T?> DeserializeResponseAsync<T>(HttpResponseMessage response, string operationName) where T : class
    {
        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("{Operation} failed with status {StatusCode}", operationName, response.StatusCode);
            return null;
        }

        var json = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<T>(json, JsonOptions);
    }

    private static string BuildReportsQueryString(Guid? userId, string? participant, int limit, int offset)
    {
        var parts = new List<string>();

        if (userId.HasValue)
            parts.Add($"userId={userId.Value}");

        if (participant != null)
            parts.Add($"participant={Uri.EscapeDataString(participant)}");

        parts.Add($"limit={limit}");
        parts.Add($"offset={offset}");

        return string.Join("&", parts);
    }

    #endregion
}
