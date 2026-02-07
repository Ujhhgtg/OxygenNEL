/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using Codexus.Development.SDK.Connection;
using Codexus.Development.SDK.Enums;
using OxygenNEL.IRC.Packet;
using Serilog;

namespace OxygenNEL.IRC;

public class IrcClient(GameConnection conn, Func<string>? tokenProvider) : IDisposable
{
    private readonly string _token = tokenProvider?.Invoke() ?? "";
    private string _roleId = string.Empty;

    private TcpLineClient? _tcp;
    private bool _welcomed;
    private bool _listShown;
    private Timer? _pingTimer;
    private volatile bool _running;

    public string RoleId => _roleId;
    public GameConnection Connection => conn;
    public event EventHandler<IrcChatEventArgs>? ChatReceived;

    public void Start(string nickName)
    {
        if (_running) return;
        _running = true;
        _roleId = nickName;
        Log.Information("[IRC] 启动: NickName={NickName}, RoleId={RoleId}", nickName, _roleId);
        Task.Run(Run);
    }

    public void Stop()
    {
        _running = false;
        _pingTimer?.Dispose();
        try
        {
            if (!string.IsNullOrEmpty(_token) && !string.IsNullOrEmpty(_roleId))
                _tcp?.Send(IrcProtocol.Delete(_token, _roleId));
        }
        catch { }
        _tcp?.Close();
    }

    public void SendChat(string playerName, string msg)
    {
        if (_tcp == null)
        {
            Log.Warning("[IRC] SendChat: TCP 未连接");
            return;
        }
        var cmd = IrcProtocol.Chat(_token, _roleId, msg);
        Log.Information("[IRC] 发送聊天: {Cmd}", cmd);
        _tcp.Send(cmd);
    }

    public void Dispose()
    {
        Stop();
        _tcp?.Dispose();
        GC.SuppressFinalize(this);
    }

    private void Run()
    {
        while (_running)
        {
            try
            {
                _tcp = new TcpLineClient(IrcProtocol.Host, IrcProtocol.Port);
                _tcp.Connect();

                _tcp.Send(IrcProtocol.Register(_token, _roleId));
                
                _pingTimer = new Timer(_ =>
                {
                    _tcp?.Send(IrcProtocol.Ping());
                    _tcp?.Send(IrcProtocol.List());
                }, null, 30000, 30000);

                while (_running)
                {
                    var line = _tcp.Read();
                    if (line == null) break;
                    Process(line);
                }
            }
            catch (Exception ex)
            {
                if (_running) Log.Warning(ex, "[IRC] 异常");
            }

            _pingTimer?.Dispose();
            _tcp?.Close();
            if (_running) Thread.Sleep(3000);
        }
    }

    private void Process(string line)
    {
        Log.Debug("[IRC] 收到: {Line}", line);
        var msg = IrcProtocol.Parse(line);
        if (msg == null) return;

        if (msg.IsOk && !_welcomed || msg.IsError && msg.Data.Contains("已注册"))
        {
            _welcomed = true;
            _tcp?.Send(IrcProtocol.List());
        }
        else if (msg.IsList)
        {
            Task.Run(async () =>
            {
                for (var i = 0; i < 10 && _running; i++)
                {
                    if (conn.State == EnumConnectionState.Play) break;
                    await Task.Delay(500);
                }
                if (_running && conn.State == EnumConnectionState.Play)
                {
                    if (!_listShown)
                    {
                        _listShown = true;
                        Msg("§a[§bIRC§a] IRC 连接成功 Ciallo～(∠・ω< )⌒");
                    }
                    if (msg.PlayerCount > 0)
                        Msg($"§e[§bIRC§e] 当前在线 {msg.PlayerCount} 人，使用 §a/irc 想说的话§e 聊天");
                }
            });
        }
        else if (msg.IsError)
        {
            Log.Warning("[IRC] 服务器错误: {Error}", msg.Data);
        }
        else if (msg.IsChatBroadcast)
        {
            ChatReceived?.Invoke(this, new IrcChatEventArgs { Message = msg.Data });
        }
    }

    private void Msg(string msg)
    {
        Log.Information("[IRC] 显示消息: {Msg}", msg);
        CChatCommandIrc.SendLocalMessage(conn, msg);
    }
}
