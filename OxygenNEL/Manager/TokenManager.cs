// 我不知道这个类的功能是什么，但是它于 8a62c23c 被移除，所以我还是把它还原了回来

using System;
using System.Collections.Generic;
using Serilog;

namespace OxygenNEL.Manager;

public class TokenManager
{
    private static TokenManager? _instance;

    private readonly Dictionary<string, string> _tokens = new();

    public static TokenManager Instance => _instance ??= new TokenManager();

    public void UpdateToken(string id, string token)
    {
        try
        {
            if (!_tokens.TryAdd(id, token))
            {
                _tokens[id] = token;
            }
        }
        catch (Exception ex)
        {
            Log.Error("Error while updating access token, {exception}", ex.Message);
        }
    }

    public void RemoveToken(string entityId)
    {
        _tokens.Remove(entityId);
    }

    public string GetToken(string entityId)
    {
        return !_tokens.TryGetValue(entityId, out string value) ? string.Empty : value;
    }
}