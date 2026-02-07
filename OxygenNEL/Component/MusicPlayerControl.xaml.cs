using System;
using System.IO;
using Windows.Foundation;
using Windows.Media.Core;
using Windows.Media.Playback;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using OxygenNEL.Manager;
using Serilog;

namespace OxygenNEL.Component;

public sealed partial class MusicPlayerControl : UserControl
{
    private MediaPlayer? _musicPlayer;
    private bool _isMusicPlaying;
    private bool _isDragging;
    private bool _isUpdatingSlider;
    private Point _dragStartPoint;
    private double _offsetX;
    private double _offsetY;

    public MusicPlayerControl()
    {
        InitializeComponent();
        MusicPlayerPanel.PointerPressed += Panel_PointerPressed;
        MusicPlayerPanel.PointerMoved += Panel_PointerMoved;
        MusicPlayerPanel.PointerReleased += Panel_PointerReleased;
    }

    public void ApplySettings()
    {
        var settings = SettingManager.Instance.Get();

        if (!settings.MusicPlayerEnabled)
        {
            MusicPlayerPanel.Visibility = Visibility.Collapsed;
            Cleanup();
            return;
        }

        MusicPlayerPanel.Visibility = Visibility.Visible;

        if (string.IsNullOrEmpty(settings.MusicPath) || !File.Exists(settings.MusicPath))
        {
            MusicTitle.Text = "未选择音乐";
            Cleanup();
            return;
        }

        try
        {
            if (_musicPlayer == null)
            {
                _musicPlayer = new MediaPlayer
                {
                    IsLoopingEnabled = true,
                    Volume = settings.MusicVolume
                };
                _musicPlayer.CommandManager.IsEnabled = false;
                _musicPlayer.MediaOpened += OnMediaOpened;
                _musicPlayer.PlaybackSession.PositionChanged += OnPositionChanged;
            }

            var fullPath = Path.GetFullPath(settings.MusicPath);
            _musicPlayer.Source = MediaSource.CreateFromUri(new Uri(fullPath));

            var volume = settings.MusicVolume;
            if (volume <= 0) volume = 0.5;
            _musicPlayer.Volume = volume;

            MusicTitle.Text = Path.GetFileNameWithoutExtension(settings.MusicPath);

            _musicPlayer.Play();
            _isMusicPlaying = true;
            UpdatePlayPauseIcon();

            Log.Information("已加载音乐: {Path}", settings.MusicPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "加载音乐失败");
            MusicTitle.Text = "加载失败";
        }
    }

    public void UpdateVolume(double volume)
    {
        if (_musicPlayer != null)
        {
            _musicPlayer.Volume = volume;
        }
    }

    private void Cleanup()
    {
        var player = _musicPlayer;
        _musicPlayer = null;
        _isMusicPlaying = false;

        if (player != null)
        {
            try { player.Pause(); } catch { }
            try { player.MediaOpened -= OnMediaOpened; } catch { }
            try { player.PlaybackSession.PositionChanged -= OnPositionChanged; } catch { }
            try { player.Source = null; } catch { }
            try { player.Dispose(); } catch { }
        }

        UpdatePlayPauseIcon();
        MusicProgressSlider.Value = 0;
        MusicTimeText.Text = "0:00";
    }

    private void MusicPlayPauseBtn_Click(object sender, RoutedEventArgs e)
    {
        if (_musicPlayer == null) return;

        if (_isMusicPlaying)
        {
            _musicPlayer.Pause();
            _isMusicPlaying = false;
        }
        else
        {
            _musicPlayer.Play();
            _isMusicPlaying = true;
        }
        UpdatePlayPauseIcon();
    }

    private void UpdatePlayPauseIcon()
    {
        MusicPlayPauseIcon.Glyph = _isMusicPlaying ? "\uE769" : "\uE768";
    }

    private void OnMediaOpened(MediaPlayer sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_musicPlayer == null) return;
            try
            {
                var duration = sender.PlaybackSession.NaturalDuration;
                if (duration.TotalSeconds > 0)
                {
                    MusicProgressSlider.Maximum = duration.TotalSeconds;
                }
            }
            catch { }
        });
    }

    private void OnPositionChanged(MediaPlaybackSession sender, object args)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            if (_musicPlayer == null) return;
            try
            {
                _isUpdatingSlider = true;
                MusicProgressSlider.Value = sender.Position.TotalSeconds;
                var pos = sender.Position;
                MusicTimeText.Text = $"{(int)pos.TotalMinutes}:{pos.Seconds:D2}";
            }
            catch { }
            finally { _isUpdatingSlider = false; }
        });
    }

    private void MusicProgressSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
    {
        if (_isUpdatingSlider || _musicPlayer == null) return;
        _musicPlayer.PlaybackSession.Position = TimeSpan.FromSeconds(e.NewValue);
    }

    private void Panel_PointerPressed(object sender, PointerRoutedEventArgs e)
    {
        if (e.OriginalSource is Button || e.OriginalSource is Slider || 
            e.OriginalSource is Thumb) return;

        _isDragging = true;
        _dragStartPoint = e.GetCurrentPoint((UIElement)Parent).Position;
        MusicPlayerPanel.CapturePointer(e.Pointer);
        e.Handled = true;
    }

    private void Panel_PointerMoved(object sender, PointerRoutedEventArgs e)
    {
        if (!_isDragging) return;

        var currentPoint = e.GetCurrentPoint((UIElement)Parent).Position;
        var deltaX = currentPoint.X - _dragStartPoint.X;
        var deltaY = currentPoint.Y - _dragStartPoint.Y;

        _offsetX += deltaX;
        _offsetY += deltaY;

        MusicPlayerTransform.X = _offsetX;
        MusicPlayerTransform.Y = _offsetY;

        _dragStartPoint = currentPoint;
        e.Handled = true;
    }

    private void Panel_PointerReleased(object sender, PointerRoutedEventArgs e)
    {
        if (_isDragging)
        {
            _isDragging = false;
            MusicPlayerPanel.ReleasePointerCapture(e.Pointer);
            e.Handled = true;
        }
    }
}
