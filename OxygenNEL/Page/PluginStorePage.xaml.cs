using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Handlers.Plugin;
using Serilog;

namespace OxygenNEL.Page;

public sealed partial class PluginStorePage : Microsoft.UI.Xaml.Controls.Page
{
    public static string PageTitle => "插件商店";

    public ObservableCollection<AvailablePluginItem> AvailablePlugins { get; } = new();

    public PluginStorePage()
    {
        InitializeComponent();
        Loaded += PluginStorePage_Loaded;
    }

    private async void PluginStorePage_Loaded(object sender, RoutedEventArgs e)
    {
        await LoadAvailablePluginsAsync();
    }

    private async Task LoadAvailablePluginsAsync()
    {
        AvailablePlugins.Clear();
        var items = await new ListAvailablePlugins().Execute();
        var installedIds = new ListInstalledPlugins().Execute().Select(p => p.Id.ToUpperInvariant()).ToHashSet();
        foreach (var item in items)
        {
            item.IsInstalled = installedIds.Contains(item.Id);
            AvailablePlugins.Add(item);
        }
    }

    private async void InstallAvailablePluginButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is AvailablePluginItem item)
            try
            {
                await InstallOneAsync(item);
                item.IsInstalled = true;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "安装插件失败");
            }
    }

    private void BackButton_Click(object sender, RoutedEventArgs e)
    {
        Frame.Navigate(typeof(PluginsPage));
    }

    private async Task InstallOneAsync(AvailablePluginItem item)
    {
        await Task.Run(() => new InstallPlugin().Execute(item).GetAwaiter().GetResult());
    }
}