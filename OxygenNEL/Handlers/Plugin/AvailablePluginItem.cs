using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OxygenNEL.Handlers.Plugin;

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