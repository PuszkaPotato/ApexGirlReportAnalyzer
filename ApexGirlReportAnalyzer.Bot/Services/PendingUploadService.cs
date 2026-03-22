namespace ApexGirlReportAnalyzer.Bot.Services;

public record PendingUploadData(
    Guid UserId,
    string DiscordServerId,
    string DiscordChannelId,
    string DiscordMessageId,
    string ImageUrl,
    string FileName,
    string? PlayerInGameId,
    int? PlayerTeamRank,
    int? PlayerServer,
    string? EnemyInGameId,
    int? EnemyTeamRank,
    int? EnemyServer);

/// <summary>
/// Stores upload data temporarily while waiting for user confirmation via Discord button.
/// </summary>
public class PendingUploadService
{
    private readonly Dictionary<string, PendingUploadData> _pending = new();

    public string Add(PendingUploadData data)
    {
        var id = Guid.NewGuid().ToString("N");
        _pending[id] = data;
        return id;
    }

    public PendingUploadData? GetAndRemove(string id)
    {
        if (_pending.TryGetValue(id, out var data))
        {
            _pending.Remove(id);
            return data;
        }
        return null;
    }
}
