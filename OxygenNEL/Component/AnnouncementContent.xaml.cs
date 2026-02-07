/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Core.Api;
using Serilog;

namespace OxygenNEL.Component;

public sealed partial class AnnouncementContent : UserControl
{
    public AnnouncementContent()
    {
        InitializeComponent();
        Loaded += AnnouncementContent_Loaded;
    }

    private async void AnnouncementContent_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            var resp = await OxygenApi.Instance.GetAnnouncementAsync();
            ContentText.Text = resp.Success && !string.IsNullOrEmpty(resp.Content) ? resp.Content : "暂无公告";
        }
        catch (Exception ex)
        {
            Log.Error(ex, "获取公告失败");
            ContentText.Text = "暂无公告";
        }
    }
}