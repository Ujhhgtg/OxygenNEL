using System.Collections.Concurrent;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace OxygenNEL.IRC;

public class IrcPlayerList
{
    private readonly ConcurrentDictionary<string, string> _players = new();

    public int Count => _players.Count;
    public IReadOnlyDictionary<string, string> All => _players;

    public void Update(string json)
    {
        var list = JsonSerializer.Deserialize<PlayerData[]>(json);
        _players.Clear();
        if (list != null)
            foreach (var p in list)
                _players[p.PlayerName] = p.Username;
    }

    public void Clear()
    {
        _players.Clear();
    }

    private class PlayerData
    {
        [JsonPropertyName("Username")] public string Username { get; set; } = "";
        [JsonPropertyName("PlayerName")] public string PlayerName { get; set; } = "";
    }
}