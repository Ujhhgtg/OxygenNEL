using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Serilog;

namespace OxygenNEL.Component;

public sealed partial class AdContent : UserControl
{
    private class AdItem
    {
        public string Text { get; set; } = string.Empty;
        public string Url { get; set; } = string.Empty;
        public string ButtonText { get; set; } = string.Empty;
    }

    private List<AdItem> _ads = new();
    private int _index;
    private DispatcherTimer? _timer;
    public AdContent()
    {
        InitializeComponent();
        InitializeAds();
        AdScroll.SizeChanged += AdScroll_SizeChanged;
        if (_ads.Count > 1)
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
            _timer.Tick += (s, e) => { Next(); };
            _timer.Start();
        }
    }

    private void OpenOfficialSiteButton_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var url = "https://freecookie.studio/";
            if (sender is Button b && b.Tag is string t && !string.IsNullOrWhiteSpace(t)) url = t;
            Process.Start(new ProcessStartInfo { FileName = url, UseShellExecute = true });
        }
        catch (Exception ex)
        {
            Log.Error(ex, "打开网站失败");
        }
    }

    private void InitializeAds()
    {
        _ads.Clear();
        _ads.Add(new AdItem { Text = "最好的客户端: Southside | 官方群1011337297", Url = "https://client.freecookie.studio/", ButtonText = "官方网站" });
        _ads.Add(new AdItem { Text = "Atri Injection 新一代安全热注入 Hyp/布吉岛全模式适用", Url = "https://shop.atri.cards/", ButtonText = "官方网站" });
        AdStack.Children.Clear();
        foreach (var ad in _ads)
        {
            var sp = new StackPanel { Spacing = 8 };
            var tb = new TextBlock { Text = ad.Text, TextWrapping = TextWrapping.Wrap };
            var btn = new Button { Content = ad.ButtonText, HorizontalAlignment = HorizontalAlignment.Left, Tag = ad.Url };
            btn.Click += OpenOfficialSiteButton_Click;
            sp.Children.Add(tb);
            sp.Children.Add(btn);
            AdStack.Children.Add(sp);
        }
        var w = AdScroll.ActualWidth;
        var reserved = 96;
        var contentWidth = w > reserved ? w - reserved : w;
        foreach (var c in AdStack.Children)
        {
            if (c is FrameworkElement fe) fe.Width = contentWidth;
        }
        UpdateDots();
    }

    private void AdScroll_SizeChanged(object sender, SizeChangedEventArgs e)
    {
        var w = AdScroll.ActualWidth;
        var reserved = 96;
        var contentWidth = w > reserved ? w - reserved : w;
        foreach (var c in AdStack.Children)
        {
            if (c is FrameworkElement fe) fe.Width = contentWidth;
        }
        UpdateView();
        UpdateDots();
    }

    private void UpdateView()
    {
        var w = AdScroll.ActualWidth;
        var x = _index * w;
        AdScroll.ChangeView(x, null, null);
        UpdateDots();
    }

    private void UpdateDots()
    {
        DotPanel.Children.Clear();
        for (var i = 0; i < _ads.Count; i++)
        {
            var el = new Ellipse
            {
                Width = 8, Height = 8, Margin = new Thickness(3),
                Fill = new SolidColorBrush(i == _index ? Colors.DodgerBlue : Colors.Gray),
                Tag = i
            };
            el.Tapped += Dot_Tapped;
            DotPanel.Children.Add(el);
        }
        PrevButton.Visibility = _ads.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        NextButton.Visibility = _ads.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
        DotPanel.Visibility = _ads.Count > 1 ? Visibility.Visible : Visibility.Collapsed;
    }

    private void Dot_Tapped(object sender, TappedRoutedEventArgs e)
    {
        if (sender is FrameworkElement fe && fe.Tag is int idx)
        {
            _index = idx;
            UpdateView();
        }
    }

    private void PrevButton_Click(object sender, RoutedEventArgs e)
    {
        if (_ads.Count == 0) return;
        _index = (_index - 1 + _ads.Count) % _ads.Count;
        UpdateView();
        ResetTimer();
    }

    private void NextButton_Click(object sender, RoutedEventArgs e)
    {
        if (_ads.Count == 0) return;
        _index = (_index + 1) % _ads.Count;
        UpdateView();
        ResetTimer();
    }

    private void Next()
    {
        if (_ads.Count == 0) return;
        _index = (_index + 1) % _ads.Count;
        UpdateView();
        ResetTimer();
    }

    private void ResetTimer()
    {
        if (_timer == null)
        {
            if (_ads.Count > 1)
            {
                _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(5) };
                _timer.Tick += (s, e) => { Next(); };
                _timer.Start();
            }
            return;
        }
        try
        {
            _timer.Stop();
            _timer.Start();
        }
        catch { }
    }
}