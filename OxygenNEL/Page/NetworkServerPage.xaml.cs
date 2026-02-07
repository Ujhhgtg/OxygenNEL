using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Windows.ApplicationModel.DataTransfer;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OxygenNEL.Component;
using OxygenNEL.Entities.Web.NetGame;
using OxygenNEL.Handlers.Game.NetServer;
using OxygenNEL.Manager;
using OxygenNEL.Utils;
using Serilog;
using static OxygenNEL.Utils.StaTaskRunner;

namespace OxygenNEL.Page;

public sealed partial class NetworkServerPage : Microsoft.UI.Xaml.Controls.Page, INotifyPropertyChanged
{
    public static string PageTitle => "网络服务器";

    private readonly SemaphoreSlim _imageLimiter = new(6);
    private CancellationTokenSource? _cts;
    private int _page = 1;
    private int _refreshId;
    private bool _hasMore;
    private bool _notLogin;
    private string? _pendingServerId;

    public ObservableCollection<ServerItem> Servers { get; } = new();
    public bool NotLogin { get => _notLogin; private set { _notLogin = value; OnPropertyChanged(nameof(NotLogin)); } }

    public NetworkServerPage()
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
                await JoinServerAsync(id, id);
            }
        };
    }

    protected override void OnNavigatedTo(NavigationEventArgs e)
    {
        base.OnNavigatedTo(e);
        if (e.Parameter is string serverId && !string.IsNullOrWhiteSpace(serverId))
            _pendingServerId = serverId;
    }

    private async Task RefreshAsync(string? keyword = null)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        var ct = _cts.Token;
        var myId = Interlocked.Increment(ref _refreshId);

        var result = await RunOnStaAsync(() =>
        {
            if (ct.IsCancellationRequested) return new ListServersResult();
            var offset = Math.Max(0, (_page - 1) * 20);
            return string.IsNullOrWhiteSpace(keyword)
                ? new ListServers().Execute(offset, 20)
                : new SearchServers().Execute(keyword, offset, 20);
        });
        if (myId != _refreshId) return;

        NotLogin = result.NotLogin;
        Servers.Clear();
        _hasMore = result.HasMore;

        if (!result.Success) { UpdatePaging(); return; }

        if (_page == 1 && DateTime.Now <= new DateTime(2026, 1, 5))
            Servers.Add(new ServerItem { EntityId = "77114517833647104", Name = "花雨庭(2026.1.5 R.I.P)" });

        foreach (var item in result.Items)
        {
            if (ct.IsCancellationRequested) break;
            Servers.Add(item);
            _ = LoadImageAsync(item, myId, ct);
        }
        UpdatePaging();
    }

    private async Task LoadImageAsync(ServerItem item, int myId, CancellationToken ct)
    {
        await _imageLimiter.WaitAsync(ct);
        try
        {
            if (myId != _refreshId || ct.IsCancellationRequested) return;
            var d = await RunOnStaAsync(() => new GetServersDetail().Execute(item.EntityId));
            if (d.Success && d.Images.Count > 0 && myId == _refreshId)
                DispatcherQueue.TryEnqueue(() => item.ImageUrl = d.Images[0]);
        }
        catch (Exception ex) { Log.Debug(ex, "加载图片失败"); }
        finally { _imageLimiter.Release(); }
    }

    private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        _page = 1;
        Servers.Clear();
        UpdatePaging();
        _ = RefreshAsync((sender as TextBox)?.Text);
    }

    private async void SpecifyServerButton_Click(object sender, RoutedEventArgs e)
    {
        var id = await DialogService.ShowInputAsync(XamlRoot, "指定服务器", "请输入服务器号");
        if (string.IsNullOrWhiteSpace(id)) { if (id != null) NotificationHost.ShowGlobal("请输入服务器号", ToastLevel.Error); return; }
        await JoinServerAsync(id, id);
    }

    private async void JoinServerButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ServerItem s }) await JoinServerAsync(s.EntityId, s.Name, s.ImageUrl);
    }

    private void PrevPageButton_Click(object sender, RoutedEventArgs e) { if (_page > 1) { _page--; _ = RefreshAsync(SearchBox?.Text); } }
    private void NextPageButton_Click(object sender, RoutedEventArgs e) { if (_hasMore) { _page++; _ = RefreshAsync(SearchBox?.Text); } }

    private async Task JoinServerAsync(string serverId, string serverName, string? imageUrl = null)
    {
        var openResult = await RunOnStaAsync(() => new OpenServer().Execute(serverId));
        if (!openResult.Success) { await DialogService.ShowErrorAsync(XamlRoot, openResult.Message ?? "打开失败"); return; }

        var accounts = UserManager.Instance.GetAuthorizedAccounts();
        var roles = openResult.Items;

        while (true)
        {
            var content = new JoinServerContent();
            content.SetAccounts(accounts.Select(a => new JoinServerContent.OptionItem { Label = a.Label, Value = a.Id }).ToList());
            content.SetRoles(roles.Select(r => new JoinServerContent.OptionItem { Label = r.Name, Value = r.Id }).ToList());
            content.AccountChanged += async id =>
            {
                await RunOnStaAsync(() => new SelectAccount().Execute(id));
                var r = await RunOnStaAsync(() => new OpenServer().ExecuteForAccount(id, serverId));
                if (r.Success) { roles = r.Items; DispatcherQueue.TryEnqueue(() => content.SetRoles(roles.Select(x => new JoinServerContent.OptionItem { Label = x.Name, Value = x.Id }).ToList())); }
            };

            var dlg = DialogService.Create(XamlRoot, "加入服务器", content, "启动", "白端");
            content.ParentDialog = dlg;
            var result = await dlg.ShowAsync();
            var accId = content.SelectedAccountId;
            var roleId = content.SelectedRoleId;

            if (result == ContentDialogResult.Primary)
            {
                if (string.IsNullOrWhiteSpace(accId) || string.IsNullOrWhiteSpace(roleId)) continue;
                await LaunchGameAsync(accId, serverId, serverName, roleId, imageUrl);
                break;
            }

            if (result == ContentDialogResult.None && content.AddRoleRequested)
            {
                var newRoles = await AddRoleAsync(serverId);
                if (newRoles != null) roles = newRoles;
                content.ResetAddRoleRequested();
                continue;
            }
            break;
        }
    }

    private async Task LaunchGameAsync(string accId, string serverId, string serverName, string roleId, string? imageUrl = null)
    {
        NotificationHost.ShowGlobal("正在准备游戏资源...", ToastLevel.Success);
        Log.Information("启动游戏: Server={Server}, Role={Role}", serverId, roleId);
        var r = await Task.Run(async () => await new JoinGame().Execute(accId, serverId, serverName, roleId));
        if (r.Success)
        {
            NotificationHost.ShowGlobal("启动成功", ToastLevel.Success);
            RecentPlayManager.Instance.AddOrUpdate(serverId, serverName, "网络服", imageUrl);
            var copyText = SettingManager.Instance.GetCopyIpText(r.Ip, r.Port);
            if (copyText != null)
            {
                var dp = new DataPackage(); dp.SetText(copyText); Clipboard.SetContent(dp); Clipboard.Flush();
                NotificationHost.ShowGlobal("地址已复制", ToastLevel.Success);
            }
        }
    }

    private async Task<List<RoleItem>?> AddRoleAsync(string serverId)
    {
        var addContent = new AddRoleContent();
        var dlg = DialogService.Create(XamlRoot, "添加角色", addContent, "添加");
        if (await dlg.ShowAsync() != ContentDialogResult.Primary || string.IsNullOrWhiteSpace(addContent.RoleName)) return null;
        var roleName = addContent.RoleName;
        var r = await RunOnStaAsync(() => new CreateRoleNamed().Execute(serverId, roleName));
        return r.Success ? r.Items : null;
    }

    private void UpdatePaging()
    {
        if (PageInfoText != null) PageInfoText.Text = $"第 {_page} 页";
        if (PrevPageButton != null) PrevPageButton.IsEnabled = _page > 1;
        if (NextPageButton != null) NextPageButton.IsEnabled = _hasMore;
    }

    public event PropertyChangedEventHandler? PropertyChanged;
    private void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}
