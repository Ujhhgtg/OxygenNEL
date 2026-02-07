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
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Windows.Media.Core;
using Windows.Media.Playback;
using Windows.Storage;
using Microsoft.UI;
using Microsoft.UI.Composition;
using Microsoft.UI.Composition.SystemBackdrops;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OxygenNEL.Component;
using OxygenNEL.Manager;
using OxygenNEL.Page;
using OxygenNEL.Utils;
using OxygenNEL.Utils.Update;
using Serilog;
using WinRT;
using WinRT.Interop;
using DispatcherQueueController = Windows.System.DispatcherQueueController;

namespace OxygenNEL;

public sealed partial class MainWindow : Window
{
    private static MainWindow? _instance;
    private AppWindow? _appWindow;
    private string _currentBackdrop = "";
    private bool _mainNavigationInitialized;
    private DesktopAcrylicController? _acrylicController;
    private SystemBackdropConfiguration? _configurationSource;
    private MediaPlayer? _mediaPlayer;
        
    public static DispatcherQueue? UIQueue => _instance?.DispatcherQueue;
    public static XamlRoot? DialogXamlRoot =>
        _instance == null ? null :
            (_instance.AuthOverlay.Visibility == Visibility.Visible ? _instance.OverlayFrame.XamlRoot : null)
            ?? _instance.ContentFrame.XamlRoot
            ?? _instance.NavView.XamlRoot;
    public static void RefreshAuthUi() => _instance?.UpdateAuthOverlay();

    public MainWindow()
    {
        InitializeComponent();
        _instance = this;
        ExtendsContentIntoTitleBar = true;
        SetTitleBar(AppTitleBar);
        var hWnd = WindowNative.GetWindowHandle(this);
        var windowId = Win32Interop.GetWindowIdFromWindow(hWnd);
        _appWindow = AppWindow.GetFromWindowId(windowId);
        _appWindow.Title = "Oxygen NEL";
        AppTitleTextBlock.Text = _appWindow.Title;
        ApplyThemeFromSettings();
        InitializeMainNavigationIfNeeded();
        AuthManager.Instance.LoadFromDisk();
        if (AuthManager.Instance.IsLoggedIn)
        {
            _ = VerifyAndAutoLoginAsync();
        }
        UpdateAuthOverlay();
        MusicPlayer.ApplySettings();
        _ = CheckUpdateAsync();
    }

    private async Task VerifyAndAutoLoginAsync()
    {
        var result = await AuthManager.Instance.TokenAuthAsync();
        DispatcherQueue.TryEnqueue(() =>
        {
            if (result.Success)
            {
                var name = string.IsNullOrWhiteSpace(AuthManager.Instance.Username) ? "用户" : AuthManager.Instance.Username;
                NotificationHost.ShowGlobal($"欢迎 {name}，已自动登录", ToastLevel.Success);
                _ = Task.Run(async () => await AuthManager.Instance.GetCrcSaltAsync());
            }
            else
            {
                AuthManager.Instance.Clear();
                NotificationHost.ShowGlobal("登录已过期，请重新登录", ToastLevel.Warning);
            }
            UpdateAuthOverlay();
        });
    }

    private async Task CheckUpdateAsync()
    {
        await UpdateManager.CheckForUpdatesAsync(this);
    }

    private void UpdateAuthOverlay()
    {
        if (AuthManager.Instance.IsLoggedIn)
        {
            AuthOverlay.Visibility = Visibility.Collapsed;
            NavView.Visibility = Visibility.Visible;
            OverlayFrame.Content = null;
            UserProfile.UpdateUserInfo();
            return;
        }

        NavView.Visibility = Visibility.Collapsed;
        AuthOverlay.Visibility = Visibility.Visible;
        if (OverlayFrame.Content == null) OverlayFrame.Navigate(typeof(LoginPage));
    }

    private static readonly Dictionary<string, (Type Page, string Title)> Pages = new()
    {
        ["HomePage"] = (typeof(HomePage), HomePage.PageTitle),
        ["AccountPage"] = (typeof(AccountPage), AccountPage.PageTitle),
        ["NetworkServerPage"] = (typeof(NetworkServerPage), NetworkServerPage.PageTitle),
        ["RentalServerPage"] = (typeof(RentalServerPage), RentalServerPage.PageTitle),
        ["PluginsPage"] = (typeof(PluginsPage), PluginsPage.PageTitle),
        ["GamesPage"] = (typeof(GamesPage), GamesPage.PageTitle),
        ["SkinPage"] = (typeof(SkinPage), SkinPage.PageTitle),
        ["ToolsPage"] = (typeof(ToolsPage), ToolsPage.PageTitle),
        ["AboutPage"] = (typeof(AboutPage), AboutPage.PageTitle),
    };

    private void NavView_Loaded(object sender, RoutedEventArgs e)
    {
        InitializeMainNavigationIfNeeded();
    }

    private void AddNavItem(Symbol icon, string key)
    {
        if (!Pages.TryGetValue(key, out var info)) return;
        NavView.MenuItems.Add(new NavigationViewItem { Icon = new SymbolIcon(icon), Content = info.Title, Tag = key });
    }

    private void InitializeMainNavigationIfNeeded()
    {
        if (_mainNavigationInitialized) return;
        _mainNavigationInitialized = true;

        NavView.MenuItems.Clear();
        AddNavItem(Symbol.Home, "HomePage");
        AddNavItem(Symbol.People, "AccountPage");
        AddNavItem(Symbol.World, "NetworkServerPage");
        AddNavItem(Symbol.Remote, "RentalServerPage");
        AddNavItem(Symbol.AllApps, "PluginsPage");
        AddNavItem(Symbol.Play, "GamesPage");
        AddNavItem(Symbol.AllApps, "SkinPage");
        AddNavItem(Symbol.Setting, "ToolsPage");
        AddNavItem(Symbol.ContactInfo, "AboutPage");

        foreach (NavigationViewItemBase item in NavView.MenuItems)
        {
            if (item is NavigationViewItem navItem && navItem.Tag?.ToString() == "HomePage")
            {
                NavView.SelectedItem = navItem;
                ContentFrame.Navigate(typeof(HomePage));
                break;
            }
        }
    }

    private void NavView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
    {
        if (args.IsSettingsSelected)
        {
            ContentFrame.Navigate(typeof(SettingsPage));
        }
        else if (args.SelectedItem is NavigationViewItem { Tag: string key } && Pages.TryGetValue(key, out var info))
        {
            ContentFrame.Navigate(info.Page);
        }
    }

    private void NavView_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        if (NavView.PaneDisplayMode == NavigationViewPaneDisplayMode.Left)
        {
            NavView.OpenPaneLength = e.NewSize.Width * 0.2; 
        }
    }

    private void ApplyThemeFromSettings()
    {
        try
        {
            var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
            var t = ElementTheme.Default;
            if (mode == "light") t = ElementTheme.Light;
            else if (mode == "dark") t = ElementTheme.Dark;
            RootGrid.RequestedTheme = t;
            NavView.RequestedTheme = t;
            ContentFrame.RequestedTheme = t;
            var actual = t == ElementTheme.Default ? RootGrid.ActualTheme : t;
            UpdateTitleBarColors(actual);

            var bd = SettingManager.Instance.Get().Backdrop?.Trim().ToLowerInvariant() ?? "mica";
            if (bd != _currentBackdrop)
            {
                var oldAcrylicController = _acrylicController;
                var oldConfigurationSource = _configurationSource;
                _acrylicController = null;
                _configurationSource = null;

                if (bd == "custom")
                {
                    ApplyCustomBackground();
                }
                else if (bd == "acrylic")
                {
                    CleanupCustomBackgroundSync();
                    TrySetCustomAcrylic(actual);
                }
                else
                {
                    CleanupCustomBackgroundSync();
                    SystemBackdrop = new MicaBackdrop();
                }
                RootGrid.Background = null;
                _currentBackdrop = bd;

                if (oldAcrylicController != null)
                {
                    oldAcrylicController.Dispose();
                }
                if (oldConfigurationSource != null)
                {
                    Activated -= Window_Activated;
                    ((FrameworkElement)Content).ActualThemeChanged -= Window_ThemeChanged;
                }
            }
            else if (bd == "acrylic" && _configurationSource != null)
            {
                UpdateAcrylicTheme(actual);
            }
            else if (bd == "custom")
            {
                ApplyCustomBackground();
            }
        }
        catch (Exception ex) { Log.Warning(ex, "应用主题失败"); }
    }

    private static readonly HashSet<string> VideoExtensions = new(StringComparer.OrdinalIgnoreCase) { ".mp4", ".webm", ".wmv", ".avi", ".mkv" };

    private async void ApplyCustomBackground()
    {
        var path = SettingManager.Instance.Get().CustomBackgroundPath;

        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            DispatcherQueue.TryEnqueue(() =>
            {
                CleanupCustomBackgroundSync();
                SystemBackdrop = new MicaBackdrop();
            });
            return;
        }

        var fullPath = Path.GetFullPath(path);
        var ext = Path.GetExtension(path);
        var isVideo = VideoExtensions.Contains(ext);

        try
        {
            if (isVideo)
            {
                DispatcherQueue.TryEnqueue(async () =>
                {
                    try
                    {
                        SystemBackdrop = null;
                        RootGrid.Background = new SolidColorBrush(Colors.Black);
                        BackgroundImage.Visibility = Visibility.Collapsed;
                        BackgroundImage.Source = null;

                        CleanupVideoPlayer();

                        var storageFile = await StorageFile.GetFileFromPathAsync(fullPath);

                        _mediaPlayer = new MediaPlayer
                        {
                            IsLoopingEnabled = true,
                            Volume = 0,
                            IsVideoFrameServerEnabled = false,
                            RealTimePlayback = true
                        };
                        _mediaPlayer.CommandManager.IsEnabled = false;

                        var mediaSource = MediaSource.CreateFromStorageFile(storageFile);
                        _mediaPlayer.Source = mediaSource;

                        BackgroundVideo.SetMediaPlayer(_mediaPlayer);
                        BackgroundVideo.Visibility = Visibility.Visible;
                        _mediaPlayer.Play();
                            
                        Log.Information("已应用自定义视频背景: {Path}", fullPath);
                    }
                    catch (Exception ex)
                    {
                        Log.Error(ex, "视频播放失败");
                        CleanupVideoPlayer();
                        SystemBackdrop = new MicaBackdrop();
                    }
                });
            }
            else
            {
                DispatcherQueue.TryEnqueue(() =>
                {
                    SystemBackdrop = null;
                    RootGrid.Background = new SolidColorBrush(Colors.Transparent);
                        
                    CleanupVideoPlayer();

                    var bitmap = new BitmapImage(new Uri(fullPath));
                    BackgroundImage.Source = bitmap;
                    BackgroundImage.Visibility = Visibility.Visible;
                        
                    Log.Information("已应用自定义图片背景: {Path}", fullPath);
                });
            }
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "加载自定义背景失败: {Path}", fullPath);
            DispatcherQueue.TryEnqueue(() =>
            {
                CleanupCustomBackgroundSync();
                SystemBackdrop = new MicaBackdrop();
            });
        }
    }

    private void CleanupVideoPlayer()
    {
        BackgroundVideo.Visibility = Visibility.Collapsed;
        if (_mediaPlayer != null)
        {
            _mediaPlayer.Pause();
            BackgroundVideo.SetMediaPlayer(null);
            _mediaPlayer.Dispose();
            _mediaPlayer = null;
        }
    }

    private void CleanupCustomBackgroundSync()
    {
        BackgroundImage.Visibility = Visibility.Collapsed;
        BackgroundImage.Source = null;
        CleanupVideoPlayer();
        RootGrid.Background = null;
    }

    private void CleanupCustomBackground()
    {
        DispatcherQueue.TryEnqueue(CleanupCustomBackgroundSync);
    }

    private void CleanupAcrylicController()
    {
        if (_acrylicController != null)
        {
            _acrylicController.Dispose();
            _acrylicController = null;
        }
        if (_configurationSource != null)
        {
            Activated -= Window_Activated;
            ((FrameworkElement)Content).ActualThemeChanged -= Window_ThemeChanged;
            _configurationSource = null;
        }
    }

    private void TrySetCustomAcrylic(ElementTheme theme)
    {
        if (!DesktopAcrylicController.IsSupported()) 
        {
            SystemBackdrop = new DesktopAcrylicBackdrop();
            return;
        }

        EnsureWindowsSystemDispatcherQueueController();

        _configurationSource = new SystemBackdropConfiguration();
        Activated += Window_Activated;
        Closed += Window_Closed;
        ((FrameworkElement)Content).ActualThemeChanged += Window_ThemeChanged;

        _configurationSource.IsInputActive = true;
        UpdateAcrylicTheme(theme);

        _acrylicController = new DesktopAcrylicController();
            
        _acrylicController.Kind = DesktopAcrylicKind.Thin;
        _acrylicController.TintOpacity = 0.0f;
        _acrylicController.LuminosityOpacity = 0.1f;
            
        _acrylicController.AddSystemBackdropTarget(this.As<ICompositionSupportsSystemBackdrop>());
        _acrylicController.SetSystemBackdropConfiguration(_configurationSource);
    }

    private DispatcherQueueController? _dispatcherQueueController;

    private void EnsureWindowsSystemDispatcherQueueController()
    {
        if (Windows.System.DispatcherQueue.GetForCurrentThread() != null) return;

        if (_dispatcherQueueController == null)
        {
            DispatcherQueueOptions options;
            options.dwSize = Marshal.SizeOf(typeof(DispatcherQueueOptions));
            options.threadType = 2;
            options.apartmentType = 2;

            CreateDispatcherQueueController(options, out var controller);
            _dispatcherQueueController = DispatcherQueueController.FromAbi(controller);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct DispatcherQueueOptions
    {
        internal int dwSize;
        internal int threadType;
        internal int apartmentType;
    }

    [DllImport("CoreMessaging.dll")]
    private static extern int CreateDispatcherQueueController(DispatcherQueueOptions options, out nint dispatcherQueueController);

    private void UpdateAcrylicTheme(ElementTheme theme)
    {
        if (_configurationSource == null) return;
        _configurationSource.Theme = theme switch
        {
            ElementTheme.Dark => SystemBackdropTheme.Dark,
            ElementTheme.Light => SystemBackdropTheme.Light,
            _ => SystemBackdropTheme.Default
        };
    }

    private void Window_Activated(object sender, WindowActivatedEventArgs args)
    {
        if (_configurationSource != null)
            _configurationSource.IsInputActive = true;
    }

    private void Window_Closed(object sender, WindowEventArgs args)
    {
        CleanupAcrylicController();
        CleanupCustomBackground();
        Closed -= Window_Closed;
    }

    private void Window_ThemeChanged(FrameworkElement sender, object args)
    {
        if (_configurationSource != null)
        {
            UpdateAcrylicTheme(((FrameworkElement)Content).ActualTheme);
        }
    }

    public static void ApplyThemeFromSettingsStatic()
    {
        _instance?.ApplyThemeFromSettings();
    }

    public static void NavigateToPageStatic(Type pageType, object? parameter = null)
    {
        if (_instance == null) return;
        _instance.DispatcherQueue.TryEnqueue(() =>
        {
            foreach (NavigationViewItemBase item in _instance.NavView.MenuItems)
            {
                if (item is NavigationViewItem navItem)
                {
                    var key = navItem.Tag?.ToString();
                    if (key != null && Pages.TryGetValue(key, out var info) && info.Page == pageType)
                    {
                        _instance.NavView.SelectedItem = navItem;
                        _instance.ContentFrame.Navigate(pageType, parameter);
                        return;
                    }
                }
            }
        });
    }

    private void UpdateTitleBarColors(ElementTheme theme)
    {
        try
        {
            var tb = _appWindow?.TitleBar;
            if (tb == null) return;
            var fg = ColorUtil.ForegroundForTheme(theme);
            var bg = ColorUtil.Transparent;
            tb.ForegroundColor = fg;
            tb.InactiveForegroundColor = fg;
            tb.ButtonForegroundColor = fg;
            tb.ButtonInactiveForegroundColor = fg;
            tb.BackgroundColor = bg;
            tb.InactiveBackgroundColor = bg;
            tb.ButtonHoverForegroundColor = fg;
            tb.ButtonPressedForegroundColor = fg;
            tb.ButtonBackgroundColor = ColorUtil.Transparent;
            tb.ButtonInactiveBackgroundColor = ColorUtil.Transparent;
            tb.ButtonHoverBackgroundColor = ColorUtil.HoverBackgroundForTheme(theme);
            tb.ButtonPressedBackgroundColor = ColorUtil.PressedBackgroundForTheme(theme);
        }
        catch (Exception ex) { Log.Warning(ex, "更新标题栏颜色失败"); }
    }

    public static void ApplyMusicPlayerSettingsStatic()
    {
        _instance?.MusicPlayer.ApplySettings();
    }

    public static void UpdateMusicVolumeStatic(double volume)
    {
        _instance?.MusicPlayer.UpdateVolume(volume);
    }
}