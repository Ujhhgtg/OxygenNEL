/*
<OxygenNEL>
Copyright (C) <2025>  <OxygenNEL>

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.
*/

using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OxygenNEL.Handlers.Plugin;

public class PluginViewModel : INotifyPropertyChanged
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string Author { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string LatestVersion { get; set; } = string.Empty;
    public string DownloadUrl { get; set; } = string.Empty;

    private bool _needUpdate;
    public bool NeedUpdate
    {
        get => _needUpdate;
        set
        {
            if (_needUpdate != value)
            {
                _needUpdate = value;
                OnPropertyChanged();
            }
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}