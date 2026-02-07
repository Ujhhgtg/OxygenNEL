using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Manager;

namespace OxygenNEL.Component;

public partial class ThemedContentDialog : ContentDialog
{
    public ThemedContentDialog()
    {
        RequestedTheme = SettingManager.Instance.GetAppTheme();
    }
}