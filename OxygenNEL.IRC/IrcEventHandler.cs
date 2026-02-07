/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using System.Collections.Concurrent;
using Codexus.Development.SDK.Connection;
using Codexus.Development.SDK.Event;
using Codexus.Development.SDK.Manager;
using Codexus.Development.SDK.Utils;
using OxygenNEL.IRC.Packet;

namespace OxygenNEL.IRC;

public static class IrcEventHandler
{
    private static readonly ConcurrentDictionary<GameConnection, bool> _processed = new();

    public static void Register(Func<string> tokenProvider)
    {
        IrcManager.TokenProvider = tokenProvider;
        IrcManager.OnClientRemoved = conn => _processed.TryRemove(conn, out _);

        foreach (var channel in MessageChannels.AllVersions)
        {
            EventManager.Instance.RegisterHandler<EventLoginSuccess>(channel, OnLoginSuccess);
        }

        EventManager.Instance.RegisterHandler<EventConnectionClosed>("channel_connection", OnConnectionClosed);
    }

    private static void OnLoginSuccess(EventLoginSuccess args)
    {
        if (!IrcManager.Enabled) return;
        
        var nickName = args.Connection.NickName;
        if (string.IsNullOrEmpty(nickName)) return;

        if (!_processed.TryAdd(args.Connection, true)) return;

        var client = IrcManager.GetOrCreate(args.Connection);
        client.ChatReceived += OnChatReceived;
        client.Start(nickName);
    }

    private static void OnConnectionClosed(EventConnectionClosed args)
    {
        IrcManager.Remove(args.Connection);
    }

    private static void OnChatReceived(object? sender, IrcChatEventArgs e)
    {
        if (sender is not IrcClient client) return;
        CChatCommandIrc.SendLocalMessage(client.Connection, e.Message);
    }
}
