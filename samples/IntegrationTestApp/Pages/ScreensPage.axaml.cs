using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;

namespace IntegrationTestApp.Pages;

public partial class ScreensPage : UserControl
{
    private Screen? _lastScreen;
    private int _onScreenChangedCounter;

    public ScreensPage()
    {
        InitializeComponent();
    }

    private void OnAttachedToVisualTree(object? sender, VisualTreeAttachmentEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window ??
                     throw new AvaloniaInternalException("ScreensPage is not attached to a Window.");
        window.Screens.Changed += OnScreenChanged;

        _onScreenChangedCounter = 0;
        ScreenOnChangedCounter.Text = _onScreenChangedCounter.ToString();
    }

    private void OnScreenChanged(object? sender, EventArgs e)
    {
        _onScreenChangedCounter++;
        ScreenOnChangedCounter.Text = _onScreenChangedCounter.ToString();
    }

    private void ScreenRefresh_Click(object? sender, RoutedEventArgs e)
    {
        var window = TopLevel.GetTopLevel(this) as Window ??
            throw new AvaloniaInternalException("ScreensPage is not attached to a Window.");
        var lastScreen = _lastScreen;
        var screen = _lastScreen = window.Screens.ScreenFromWindow(window);
        ScreenName.Text = screen?.DisplayName;
        ScreenHandle.Text = screen?.TryGetPlatformHandle()?.ToString();
        ScreenBounds.Text = screen?.Bounds.ToString();
        ScreenWorkArea.Text = screen?.WorkingArea.ToString();
        ScreenScaling.Text = screen?.Scaling.ToString(CultureInfo.InvariantCulture);
        ScreenOrientation.Text = screen?.CurrentOrientation.ToString();
        ScreenSameReference.Text = ReferenceEquals(lastScreen, screen).ToString();
    }

    private void UpdateViewOnly_OnClick(object? sender, RoutedEventArgs e)
    {
        var screen = _lastScreen;
        ScreenName.Text = screen?.DisplayName;
        ScreenHandle.Text = screen?.TryGetPlatformHandle()?.ToString();
        ScreenBounds.Text = screen?.Bounds.ToString();
        ScreenWorkArea.Text = screen?.WorkingArea.ToString();
        ScreenScaling.Text = screen?.Scaling.ToString(CultureInfo.InvariantCulture);
        ScreenOrientation.Text = screen?.CurrentOrientation.ToString();
        ScreenSameReference.Text = ReferenceEquals(_lastScreen, screen).ToString();
    }
}
