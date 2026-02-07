namespace OxygenNEL.IRC;

public static class IrcProtocol
{
    public const string Host = "api.fandmc.cn";
    public const int Port = 9527;

    public static string Register(string token, string roleId) 
        => $"REGISTER {token} {roleId}";

    public static string Get(string token, string roleId) 
        => $"GET {token} {roleId}";

    public static string Delete(string token, string roleId) 
        => $"DELETE {token} {roleId}";

    public static string Chat(string token, string roleId, string msg) 
        => $"CHAT {token} {roleId} {msg}";

    public static string List() => "LIST";
    public static string Ping() => "PING";
    public static string Quit() => "QUIT";

    public static IrcMessage? Parse(string line)
    {
        if (string.IsNullOrWhiteSpace(line)) return null;

        var msg = new IrcMessage { Raw = line };

        if (line.StartsWith("OK"))
        {
            msg.Type = "OK";
            msg.Data = line.Length > 3 ? line[3..].Trim() : "";
        }
        else if (line.StartsWith("ERROR"))
        {
            msg.Type = "ERROR";
            msg.Data = line.Length > 6 ? line[6..].Trim() : "";
        }
        else if (line.StartsWith("PONG"))
        {
            msg.Type = "PONG";
        }
        else if (line.StartsWith("BYE"))
        {
            msg.Type = "BYE";
        }
        else if (line.StartsWith("LIST"))
        {
            msg.Type = "LIST";
            var parts = line.Split(' ', 3);
            if (parts.Length >= 2) msg.Data = parts[1];
            if (parts.Length >= 3) msg.Players = parts[2].Split(',');
        }
        else if (line.StartsWith("CHAT_BROADCAST "))
        {
            msg.Type = "CHAT_BROADCAST";
            msg.Data = line[15..];
        }
        else
        {
            msg.Type = "UNKNOWN";
            msg.Data = line;
        }

        return msg;
    }
}

public class IrcMessage
{
    public string Type { get; set; } = "";
    public string Data { get; set; } = "";
    public string Raw { get; set; } = "";
    public string[] Players { get; set; } = Array.Empty<string>();

    public bool IsOk => Type == "OK";
    public bool IsError => Type == "ERROR";
    public bool IsChatBroadcast => Type == "CHAT_BROADCAST";
    public bool IsPong => Type == "PONG";
    public bool IsList => Type == "LIST";
    public int PlayerCount => int.TryParse(Data, out var c) ? c : 0;
}
