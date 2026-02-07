using System;
using Windows.System;
using Microsoft.UI.Xaml;
using OxygenNEL.type;

namespace OxygenNEL.Page;

public sealed partial class AboutPage : Microsoft.UI.Xaml.Controls.Page
{
    public static string PageTitle => "关于";

    public static string AppVersion => AppInfo.AppVersion;

    public AboutPage()
    {
        InitializeComponent();
    }

    private async void License_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://www.gnu.org/licenses/gpl-3.0-standalone.html"));
    }

    private async void Github_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://github.com/Ujhhgtg/Oxygen"));
    }
}