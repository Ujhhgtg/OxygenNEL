using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Windows.System;
using Windows.UI;
using Microsoft.UI.Text;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using OxygenNEL.Manager;

namespace OxygenNEL.Component;

public sealed partial class UserProfileCard : UserControl
{
    public UserProfileCard()
    {
        InitializeComponent();
        Loaded += UserProfileCard_Loaded;
    }

    private void UserProfileCard_Loaded(object sender, RoutedEventArgs e)
    {
        UpdateUserInfo();
    }

    public void UpdateUserInfo()
    {
        UsernameText.Text = "--- Ujhhgtg ---";
        UserIdText.Text = "ID: 1337";
        UpdateAvatar();
        UpdateRank();
    }

    private void UpdateRank()
    {
        RankPanel.Children.Clear();
        var segments = ParseMinecraftColors("§3ADMINISTRATOR");
        foreach (var (content, color) in segments)
            RankPanel.Children.Add(new TextBlock
            {
                Text = content,
                FontSize = 11,
                FontWeight = FontWeights.SemiBold,
                Foreground = new SolidColorBrush(color)
            });
        RankPanel.Visibility = Visibility.Visible;
    }

    private static List<(string Text, Color Color)> ParseMinecraftColors(string text)
    {
        var result = new List<(string, Color)>();
        var currentColor = Color.FromArgb(255, 255, 255, 255);
        var buffer = "";

        for (var i = 0; i < text.Length; i++)
            if (text[i] == '§' && i + 1 < text.Length)
            {
                if (buffer.Length > 0)
                {
                    result.Add((buffer, currentColor));
                    buffer = "";
                }

                currentColor = GetMinecraftColor(text[i + 1]);
                i++;
            }
            else
            {
                buffer += text[i];
            }

        if (buffer.Length > 0) result.Add((buffer, currentColor));
        return result;
    }

    private static Color GetMinecraftColor(char code)
    {
        return char.ToLower(code) switch
        {
            '0' => Color.FromArgb(255, 0, 0, 0),
            '1' => Color.FromArgb(255, 0, 0, 170),
            '2' => Color.FromArgb(255, 0, 170, 0),
            '3' => Color.FromArgb(255, 0, 170, 170),
            '4' => Color.FromArgb(255, 170, 0, 0),
            '5' => Color.FromArgb(255, 170, 0, 170),
            '6' => Color.FromArgb(255, 255, 170, 0),
            '7' => Color.FromArgb(255, 170, 170, 170),
            '8' => Color.FromArgb(255, 85, 85, 85),
            '9' => Color.FromArgb(255, 85, 85, 255),
            'a' => Color.FromArgb(255, 85, 255, 85),
            'b' => Color.FromArgb(255, 85, 255, 255),
            'c' => Color.FromArgb(255, 255, 85, 85),
            'd' => Color.FromArgb(255, 255, 85, 255),
            'e' => Color.FromArgb(255, 255, 255, 85),
            'f' => Color.FromArgb(255, 255, 255, 255),
            _ => Color.FromArgb(255, 255, 255, 255)
        };
    }

    private void UpdateAvatar()
    {
        ShowDefaultAvatar();
    }

    private void ShowDefaultAvatar()
    {
        AvatarImageEllipse.Visibility = Visibility.Collapsed;
        AvatarEllipse.Visibility = Visibility.Visible;
        AvatarIcon.Visibility = Visibility.Visible;
    }

    private void AvatarButton_Click(object sender, RoutedEventArgs e)
    {
    }

    private void ManageButton_Click(object sender, RoutedEventArgs e)
    {
        NotificationHost.ShowGlobal("lol", ToastLevel.Normal);
    }
}