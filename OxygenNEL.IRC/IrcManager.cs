using System.Collections.Concurrent;
using Codexus.Development.SDK.Connection;
using Serilog;

namespace OxygenNEL.IRC;

public class IrcChatEventArgs : EventArgs
{
    public string Message { get; set; } = string.Empty;
}

public static class IrcManager
{
    private static readonly ConcurrentDictionary<GameConnection, IrcClient> _clients = new();

    public static bool Enabled { get; set; } = true;
    public static Func<string>? TokenProvider { get; set; }
    public static Action<GameConnection>? OnClientRemoved { get; set; }

    public static IrcClient GetOrCreate(GameConnection conn)
    {
        return _clients.GetOrAdd(conn, c => new IrcClient(c, TokenProvider));
    }

    public static IrcClient? Get(GameConnection conn)
    {
        return _clients.TryGetValue(conn, out var client) ? client : null;
    }

    public static void Remove(GameConnection conn)
    {
        if (_clients.TryRemove(conn, out var client))
        {
            client.Dispose();
            OnClientRemoved?.Invoke(conn);
            Log.Information("[IRC] 已移除: {NickName}", conn.NickName);
        }
    }

    public static void Clear()
    {
        foreach (var kv in _clients) kv.Value.Dispose();
        _clients.Clear();
    }
}