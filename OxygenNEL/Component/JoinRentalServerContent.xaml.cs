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
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Manager;

namespace OxygenNEL.Component;

public sealed partial class JoinRentalServerContent : UserControl
{
    public bool AddRoleRequested { get; private set; }
    public ContentDialog? ParentDialog { get; set; }

    public JoinRentalServerContent()
    {
        InitializeComponent();
        try
        {
            var mode = SettingManager.Instance.Get().ThemeMode?.Trim().ToLowerInvariant() ?? "system";
            var t = ElementTheme.Default;
            if (mode == "light") t = ElementTheme.Light;
            else if (mode == "dark") t = ElementTheme.Dark;
            RequestedTheme = t;
        }
        catch { }
    }

    public class OptionItem
    {
        public string Label { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;
    }

    public void SetAccounts(List<OptionItem> items)
    {
        AccountCombo.ItemsSource = items;
        if (AccountCombo.SelectedIndex < 0 && items != null && items.Count > 0)
            AccountCombo.SelectedIndex = 0;
    }

    public void SetRoles(List<OptionItem> items)
    {
        RoleCombo.ItemsSource = items;
        if (RoleCombo.SelectedIndex < 0 && items != null && items.Count > 0)
            RoleCombo.SelectedIndex = 0;
    }

    public void SetPasswordRequired(bool required)
    {
        PasswordPanel.Visibility = required ? Visibility.Visible : Visibility.Collapsed;
    }

    public string SelectedAccountId => AccountCombo.SelectedValue as string ?? string.Empty;
    public string SelectedRoleId => RoleCombo.SelectedValue as string ?? string.Empty;
    public string Password => PasswordBox.Password ?? string.Empty;

    public event Action<string>? AccountChanged;
    public event Action? AddRoleClicked;

    private void AccountCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        var id = SelectedAccountId;
        AccountChanged?.Invoke(id);
    }

    private void AddRole_Click(object sender, RoutedEventArgs e)
    {
        AddRoleRequested = true;
        ParentDialog?.Hide();
        AddRoleClicked?.Invoke();
    }

    public void ResetAddRoleRequested()
    {
        AddRoleRequested = false;
    }
}