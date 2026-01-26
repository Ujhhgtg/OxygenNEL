/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Codexus.Development.SDK.Entities;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OxygenNEL.Component;
using OxygenNEL.Entities.Web.RentalGame;
using OxygenNEL.Handlers.Game.RentalServer;
using OxygenNEL.Manager;
using OxygenNEL.Utils;
using Serilog;
using static OxygenNEL.Utils.StaTaskRunner;

namespace OxygenNEL.Page;

public sealed partial class RentalServerPage : Microsoft.UI.Xaml.Controls.Page, INotifyPropertyChanged
{
    public static string PageTitle => "租赁服";

    public ObservableCollection<RentalServerItem> Servers { get; } = new();
    CancellationTokenSource? _cts;
    int _refreshId;
    bool _notLogin;
    string? _pendingServerId;

    public bool NotLogin { get => _notLogin; private set { _notLogin = value; OnPropertyChanged(nameof(NotLogin)); } }

    public RentalServerPage()
    {
        InitializeComponent();
        DataContext = this;
        Loaded += async (_, _) =>
        {
            await RefreshAsync();
            if (_pendingServerId != null)
            {
                var id = _pendingServerId;
                _pendingServerId = null;
                await JoinServerByIdAsync(id, id);
            }
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string serverId && !string.IsNullOrWhiteSpace(serverId))
            _pendingServerId = serverId;
    }

    async void RefreshButton_Click(object sender, RoutedEventArgs e) => await RefreshAsync();

    async void SpecifyServerButton_Click(object sender, RoutedEventArgs e)
    {
        var id = await DialogService.ShowInputAsync(XamlRoot, "指定服务器", "请输入服务器号");
        if (string.IsNullOrWhiteSpace(id)) { if (id != null) NotificationHost.ShowGlobal("请输入服务器号", ToastLevel.Error); return; }
        await JoinServerByIdAsync(id, id);
    }

    async Task JoinServerByIdAsync(string serverId, string serverName)
    {
        try
        {
            var r = await RunOnStaAsync(() => new OpenRentalServer().Execute(serverId));
            if (!r.Success) { NotificationHost.ShowGlobal(r.Message ?? "打开失败", ToastLevel.Error); return; }

            var accounts = UserManager.Instance.GetAuthorizedAccounts();
            var acctItems = accounts.Select(a => new JoinRentalServerContent.OptionItem { Label = a.Label, Value = a.Id }).ToList();
            var roleItems = r.Items.Select(x => new JoinRentalServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();

            await ShowJoinDialogAsync(serverId, serverName, acctItems, roleItems, false);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "指定服务器失败");
            NotificationHost.ShowGlobal("加入失败: " + ex.Message, ToastLevel.Error);
        }
    }

    async Task RefreshAsync()
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        var myId = Interlocked.Increment(ref _refreshId);

        ListRentalServersResult result;
        try
        {
            result = await RunOnStaAsync(() =>
            {
                if (ct.IsCancellationRequested) return new ListRentalServersResult();
                return new ListRentalServers().Execute(0, 100);
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "[RentalServer] RefreshAsync 异常");
            NotLogin = false;
            Servers.Clear();
            return;
        }

        if (myId != _refreshId) return;

        if (result.NotLogin)
        {
            NotLogin = true;
            Servers.Clear();
            return;
        }

        NotLogin = false;
        Servers.Clear();
        foreach (var item in result.Items)
        {
            if (myId != _refreshId || ct.IsCancellationRequested) break;
            Servers.Add(item);
        }
    }

    private async void JoinServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is RentalServerItem s)
        {
            try
            {
                var r = await RunOnStaAsync(() => new OpenRentalServer().Execute(s.EntityId));
                if (!r.Success) return;

                var accounts = UserManager.Instance.GetAuthorizedAccounts();
                var acctItems = accounts.Select(a => new JoinRentalServerContent.OptionItem { Label = a.Label, Value = a.Id }).ToList();
                var roleItems = r.Items.Select(x => new JoinRentalServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();

                await ShowJoinDialogAsync(s.EntityId, s.Name, acctItems, roleItems, s.HasPassword, s.McVersion);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "加入租赁服失败");
                NotificationHost.ShowGlobal("加入失败: " + ex.Message, ToastLevel.Error);
            }
        }
    }

    async Task ShowJoinDialogAsync(string serverId, string serverName, List<JoinRentalServerContent.OptionItem> acctItems, List<JoinRentalServerContent.OptionItem> roleItems, bool hasPassword, string? mcVersion = null)
    {
        while (true)
        {
            var joinContent = new JoinRentalServerContent();
            joinContent.SetAccounts(acctItems);
            joinContent.SetRoles(roleItems);
            joinContent.SetPasswordRequired(hasPassword);
            joinContent.AccountChanged += async (accountId) =>
            {
                try
                {
                    await RunOnStaAsync(() => new Handlers.Game.NetServer.SelectAccount().Execute(accountId));
                    var rAcc = await RunOnStaAsync(() => new OpenRentalServer().ExecuteForAccount(accountId, serverId));
                    if (rAcc.Success)
                    {
                        var newRoles = rAcc.Items.Select(x => new JoinRentalServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();
                        DispatcherQueue.TryEnqueue(() => joinContent.SetRoles(newRoles));
                    }
                }
                catch (Exception ex) { Log.Debug(ex, "切换账号失败"); }
            };

            var dlg = DialogService.Create(XamlRoot, "加入租赁服", joinContent, "启动", null, "关闭");
            joinContent.ParentDialog = dlg;

            var result = await dlg.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                var accId = joinContent.SelectedAccountId;
                var roleId = joinContent.SelectedRoleId;
                var password = joinContent.Password;
                if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId)) continue;
                if (hasPassword && string.IsNullOrWhiteSpace(password))
                {
                    NotificationHost.ShowGlobal("请输入服务器密码", ToastLevel.Error);
                    continue;
                }

                NotificationHost.ShowGlobal("正在准备游戏资源，请稍后", ToastLevel.Success);
                await RunOnStaAsync(() => new Handlers.Game.NetServer.SelectAccount().Execute(accId));

                var req = new EntityJoinRentalGame
                {
                    ServerId = serverId,
                    ServerName = serverName,
                    Role = roleId,
                    GameId = serverId,
                    Password = password,
                    McVersion = mcVersion ?? string.Empty
                };
                var set = SettingManager.Instance.Get();
                var enabled = set?.Socks5Enabled ?? false;
                req.Socks5 = (!enabled || string.IsNullOrWhiteSpace(set?.Socks5Address))
                    ? new EntitySocks5 { Address = string.Empty, Port = 0, Username = string.Empty, Password = string.Empty }
                    : new EntitySocks5 { Enabled = true, Address = set!.Socks5Address, Port = set.Socks5Port, Username = set.Socks5Username, Password = set.Socks5Password };

                var rStart = await Task.Run(async () => await new JoinRentalGame().Execute(req));
                if (rStart.Success)
                {
                    NotificationHost.ShowGlobal("启动成功", ToastLevel.Success);
                    RecentPlayManager.Instance.AddOrUpdate(serverId, serverName, "租赁服");
                    var copyText = SettingManager.Instance.GetCopyIpText(rStart.Ip, rStart.Port);
                    if (copyText != null)
                    {
                        var dp = new DataPackage(); dp.SetText(copyText); Clipboard.SetContent(dp); Clipboard.Flush();
                        NotificationHost.ShowGlobal("地址已复制", ToastLevel.Success);
                    }
                }
                else
                {
                    NotificationHost.ShowGlobal(rStart.Message ?? "启动失败", ToastLevel.Error);
                }
                break;
            }
            else if (result == ContentDialogResult.None && joinContent.AddRoleRequested)
            {
                var addRoleContent = new AddRoleContent();
                var dlg2 = DialogService.Create(XamlRoot, "添加角色", addRoleContent, "添加", null, "关闭");
                var addRes = await dlg2.ShowAsync();
                if (addRes == ContentDialogResult.Primary && !string.IsNullOrWhiteSpace(addRoleContent.RoleName))
                {
                    var roleName = addRoleContent.RoleName;
                    var accId2 = joinContent.SelectedAccountId;
                    if (!string.IsNullOrWhiteSpace(accId2))
                        await RunOnStaAsync(() => new Handlers.Game.NetServer.SelectAccount().Execute(accId2));
                    var r2 = await RunOnStaAsync(() => new CreateRentalRole().Execute(serverId, roleName));
                    if (r2.Success)
                    {
                        roleItems = r2.Items.Select(x => new JoinRentalServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList();
                        NotificationHost.ShowGlobal("角色创建成功", ToastLevel.Success);
                    }
                }
                joinContent.ResetAddRoleRequested();
                continue;
            }
            else
            {
                break;
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
