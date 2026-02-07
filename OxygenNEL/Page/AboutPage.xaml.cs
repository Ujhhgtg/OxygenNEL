/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using System;
using Windows.System;
using Microsoft.UI.Xaml;
using OxygenNEL.type;

namespace OxygenNEL.Page;

public sealed partial class AboutPage : Microsoft.UI.Xaml.Controls.Page
{
    public static string PageTitle => "关于";
        
    public string AppVersion => AppInfo.AppVersion;

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
        await Launcher.LaunchUriAsync(new Uri("https://github.com/OxygenNEL/oxygen"));
    }

    private async void QQGroup_Click(object sender, RoutedEventArgs e)
    {
        await Launcher.LaunchUriAsync(new Uri("https://qm.qq.com/q/vF9XAxuoUK"));
    }
}