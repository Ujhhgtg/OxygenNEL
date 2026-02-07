using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OxygenNEL.Manager;
using OxygenNEL.Page;
using OxygenNEL.type;

namespace OxygenNEL.Component;

public sealed partial class RecentPlayContent : UserControl
{
    public RecentPlayContent()
    {
        InitializeComponent();
        Loaded += (_, _) => Refresh();
    }

    public void Refresh()
    {
        var items = RecentPlayManager.Instance.GetRecentItems();
        if (items.Count > 0)
        {
            RecentPlayRepeater.ItemsSource = items;
            RootPanel.Visibility = Visibility.Visible;
        }
        else
        {
            RootPanel.Visibility = Visibility.Collapsed;
        }
    }

    private void JoinButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn && btn.Tag is RecentPlayItem item)
        {
            var accounts = UserManager.Instance.GetAuthorizedAccounts();
            if (accounts.Count == 0)
            {
                NotificationHost.ShowGlobal("请先登录网易账号", ToastLevel.Warning);
                MainWindow.NavigateToPageStatic(typeof(AccountPage));
                return;
            }

            if (item.ServerType == "租赁服")
            {
                MainWindow.NavigateToPageStatic(typeof(RentalServerPage), item.ServerId);
            }
            else if (item.ServerType == "网络服")
            {
                MainWindow.NavigateToPageStatic(typeof(NetworkServerPage), item.ServerId);
            }
        }
    }
}
