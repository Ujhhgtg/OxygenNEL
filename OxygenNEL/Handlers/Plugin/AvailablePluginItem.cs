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

namespace OxygenNEL.Handlers.Plugin
{
    public class AvailablePluginItem : INotifyPropertyChanged
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Version { get; set; } = string.Empty;
        public string LogoUrl { get; set; } = string.Empty;
        public string ShortDescription { get; set; } = string.Empty;
        public string Publisher { get; set; } = string.Empty;
        public string DownloadUrl { get; set; } = string.Empty;
        public string Depends { get; set; } = string.Empty;

        private bool _isInstalled;
        public bool IsInstalled
        {
            get => _isInstalled;
            set
            {
                if (_isInstalled != value)
                {
                    _isInstalled = value;
                    OnPropertyChanged();
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
