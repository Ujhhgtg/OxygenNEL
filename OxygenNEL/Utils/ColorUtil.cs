using System;
using Windows.UI;
using Microsoft.UI;
using Microsoft.UI.Xaml;

namespace OxygenNEL.Utils;

public static class ColorUtil
{
    public static Color ParseHex(string? hex)
    {
        var s = hex?.Trim('#').Trim() ?? string.Empty;
        byte a = 255, r = 0, g = 0, b = 0;
        switch (s.Length)
        {
            case 6:
                r = Convert.ToByte(s[..2], 16);
                g = Convert.ToByte(s.Substring(2, 2), 16);
                b = Convert.ToByte(s.Substring(4, 2), 16);
                break;
            case 8:
                a = Convert.ToByte(s[..2], 16);
                r = Convert.ToByte(s.Substring(2, 2), 16);
                g = Convert.ToByte(s.Substring(4, 2), 16);
                b = Convert.ToByte(s.Substring(6, 2), 16);
                break;
        }
        return Color.FromArgb(a, r, g, b);
    }

    public static Color ForegroundForTheme(ElementTheme theme)
    {
        return theme == ElementTheme.Dark ? Colors.White : Colors.Black;
    }

    public static Color Transparent => Colors.Transparent;

    public static Color HoverBackgroundForTheme(ElementTheme theme)
    {
        return theme == ElementTheme.Dark ? Color.FromArgb(32, 255, 255, 255) : Color.FromArgb(32, 0, 0, 0);
    }

    public static Color PressedBackgroundForTheme(ElementTheme theme)
    {
        return theme == ElementTheme.Dark ? Color.FromArgb(64, 255, 255, 255) : Color.FromArgb(64, 0, 0, 0);
    }
}