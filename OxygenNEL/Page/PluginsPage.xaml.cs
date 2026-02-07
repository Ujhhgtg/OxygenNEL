using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Handlers.Plugin;
using OxygenNEL.Utils;
using Serilog;

namespace OxygenNEL.Page;

public sealed partial class PluginsPage : Microsoft.UI.Xaml.Controls.Page
{
    public static string PageTitle => "插件";

    public ObservableCollection<PluginViewModel> Plugins { get; } = new();

    public PluginsPage()
    {
        InitializeComponent();
        LoadPlugins();
    }

    private void LoadPlugins()
    {
        Plugins.Clear();
        var list = new ListInstalledPlugins().Execute();
        foreach (var item in list)
        {
            Plugins.Add(item);
        }
    }

    private void RestartButton_Click(object sender, RoutedEventArgs e)
    {
        new RestartGateway().Execute();
    }

    private void UninstallPluginButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is PluginViewModel plugin)
        {
            try
            {
                new UninstallPlugin().Execute(plugin.Id);
                Plugins.Remove(plugin);
            }
            catch (Exception ex)
            {
                Log.Error(ex, "Uninstall failed");
            }
        }
    }

    private void OpenPluginsFolderButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var dir = FileUtil.GetPluginDirectory();
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }
            Process.Start(new ProcessStartInfo
            {
                FileName = dir,
                UseShellExecute = true
            });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开插件目录失败");
        }
    }
    private void OpenStoreButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PluginStorePage));
    }
}