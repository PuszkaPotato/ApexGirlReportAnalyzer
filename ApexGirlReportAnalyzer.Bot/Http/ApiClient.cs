using System.Net.Http.Json;
using System.Text.Json;
using ApexGirlReportAnalyzer.Models.DTOs;
using ApexGirlReportAnalyzer.Models.Enums;

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
        string? playerInGameId = null,
        string? enemyInGameId = null,
        int? playerTeamRank = null,
        int? enemyTeamRank = null,
        int? playerServer = null,
        int? enemyServer = null,
        PrivacyScope privacyScope = PrivacyScope.Public,
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
        if (playerInGameId != null)
            content.Add(new StringContent(playerInGameId), "playerInGameId");
        if (enemyInGameId != null)
            content.Add(new StringContent(enemyInGameId), "enemyInGameId");
        if (playerTeamRank.HasValue)
            content.Add(new StringContent(playerTeamRank.Value.ToString()), "playerTeamRank");
        if (enemyTeamRank.HasValue)
            content.Add(new StringContent(enemyTeamRank.Value.ToString()), "enemyTeamRank");
        if (playerServer.HasValue)
            content.Add(new StringContent(playerServer.Value.ToString()), "playerServer");
        if (enemyServer.HasValue)
            content.Add(new StringContent(enemyServer.Value.ToString()), "enemyServer");
        content.Add(new StringContent(privacyScope.ToString()), "privacyScope");

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
        string? battleType = null,
        string? groupTag = null,
        string? inGameId = null,
        DateTime? battleDate = null,
        int limit = 10,
        int offset = 0,
        string? requestingDiscordUserId = null,
        string? requestingDiscordServerId = null,
        bool requestingHasAllowedRole = false,
        CancellationToken cancellationToken = default)
    {
        var query = BuildReportsQueryString(userId, participant, battleType, groupTag, inGameId, battleDate, limit, offset, requestingDiscordUserId, requestingDiscordServerId, requestingHasAllowedRole);
        _logger.LogDebug("Querying battle reports: {Query}", query);

        var response = await _httpClient.GetAsync($"api/battlereport?{query}", cancellationToken);

        return await DeserializeResponseAsync<BattleReportListResponse>(response, nameof(GetBattleReportsAsync));
    }

    /// <summary>
    /// Exports battle reports as a CSV stream.
    /// </summary>
    public async Task<Stream?> ExportBattleReportsCsvAsync(
        string? requestingDiscordUserId = null,
        string? participant = null,
        string? battleType = null,
        DateTime? battleDate = null,
        string? groupTag = null,
        CancellationToken cancellationToken = default)
    {
        var parts = new List<string>();
        if (requestingDiscordUserId != null) parts.Add($"requestingDiscordUserId={Uri.EscapeDataString(requestingDiscordUserId)}");
        if (participant != null) parts.Add($"participant={Uri.EscapeDataString(participant)}");
        if (battleType != null) parts.Add($"battleType={Uri.EscapeDataString(battleType)}");
        if (battleDate.HasValue) parts.Add($"battleDate={battleDate.Value:yyyy-MM-dd}");
        if (groupTag != null) parts.Add($"groupTag={Uri.EscapeDataString(groupTag)}");

        var query = string.Join("&", parts);
        var response = await _httpClient.GetAsync($"api/battlereport/export?{query}", cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            _logger.LogWarning("{Operation} failed with status {StatusCode}", nameof(ExportBattleReportsCsvAsync), response.StatusCode);
            return null;
        }

        return await response.Content.ReadAsStreamAsync(cancellationToken);
    }

    /// <summary>
    /// Gets a single battle report by its ID.
    /// </summary>
    public async Task<BattleReportResponse?> GetBattleReportByIdAsync(Guid reportId, string? requestingDiscordUserId = null, string? requestingDiscordServerId = null, bool requestingHasAllowedRole = false, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Getting battle report {ReportId}", reportId);

        var url = $"api/battlereport/reportId?reportId={reportId}";
        if (requestingDiscordUserId != null)
            url += $"&requestingDiscordUserId={Uri.EscapeDataString(requestingDiscordUserId)}";
        if (requestingDiscordServerId != null)
            url += $"&requestingDiscordServerId={Uri.EscapeDataString(requestingDiscordServerId)}";
        url += $"&requestingHasAllowedRole={requestingHasAllowedRole}";

        var response = await _httpClient.GetAsync(url, cancellationToken);

        return await DeserializeResponseAsync<BattleReportResponse>(response, nameof(GetBattleReportByIdAsync));
    }

    /// <summary>
    /// Creates a new tier.
    /// </summary>
    public async Task<TierResponse?> CreateTierAsync(CreateTierRequest request, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Creating tier {TierName}", request.Name);

        var response = await _httpClient.PostAsJsonAsync("api/tiers", request, cancellationToken);

        return await DeserializeResponseAsync<TierResponse>(response, nameof(CreateTierAsync));
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
    /// Assigns a tier to a Discord server.
    /// </summary>
    public async Task<bool> AssignTierToServerAsync(string discordServerId, Guid tierId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Assigning tier {TierId} to server {DiscordServerId}", tierId, discordServerId);

        var response = await _httpClient.PutAsync(
            $"api/tiers/{tierId}/assign-server/{Uri.EscapeDataString(discordServerId)}",
            null,
            cancellationToken);

        return response.IsSuccessStatusCode;
    }

    /// <summary>
    /// Assigns a tier to a user by their Discord ID.
    /// </summary>
    public async Task<(bool success, bool userNotFound)> AssignTierToUserAsync(string discordUserId, Guid tierId, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Assigning tier {TierId} to user {DiscordUserId}", tierId, discordUserId);

        var response = await _httpClient.PutAsync(
            $"api/tiers/{tierId}/assign-user/{Uri.EscapeDataString(discordUserId)}",
            null,
            cancellationToken);

        if (response.IsSuccessStatusCode) return (true, false);
        if (response.StatusCode == System.Net.HttpStatusCode.NotFound) return (false, true);
        return (false, false);
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

    private static string BuildReportsQueryString(Guid? userId, string? participant, string? battleType, string? groupTag, string? inGameId, DateTime? battleDate, int limit, int offset, string? requestingDiscordUserId = null, string? requestingDiscordServerId = null, bool requestingHasAllowedRole = false)
    {
        var parts = new List<string>();

        if (userId.HasValue)
            parts.Add($"userId={userId.Value}");
        if (participant != null)
            parts.Add($"participant={Uri.EscapeDataString(participant)}");
        if (battleType != null)
            parts.Add($"battleType={Uri.EscapeDataString(battleType)}");
        if (groupTag != null)
            parts.Add($"groupTag={Uri.EscapeDataString(groupTag)}");
        if (inGameId != null)
            parts.Add($"inGameId={Uri.EscapeDataString(inGameId)}");
        if (battleDate.HasValue)
            parts.Add($"battleDate={battleDate.Value:yyyy-MM-dd}");
        if (requestingDiscordUserId != null)
            parts.Add($"requestingDiscordUserId={Uri.EscapeDataString(requestingDiscordUserId)}");
        if (requestingDiscordServerId != null)
            parts.Add($"requestingDiscordServerId={Uri.EscapeDataString(requestingDiscordServerId)}");

        parts.Add($"requestingHasAllowedRole={requestingHasAllowedRole}");
        parts.Add($"limit={limit}");
        parts.Add($"offset={offset}");

        return string.Join("&", parts);
    }

    #endregion
}
