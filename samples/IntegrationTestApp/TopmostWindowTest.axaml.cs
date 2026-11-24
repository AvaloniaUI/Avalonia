using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;

namespace IntegrationTestApp;

public partial class TopmostWindowTest : Window
{
    private readonly DispatcherTimer? _timer;

    public TopmostWindowTest(string name)
    {
        Name = name;
        InitializeComponent();
        PositionChanged += (s, e) => CurrentPosition.Text = $"{Position}";

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(250) };
            _timer.Tick += TimerOnTick;
            _timer.Start();
        }
    }

    protected override void OnClosed(EventArgs e)
    {
        base.OnClosed(e);
        _timer?.Stop();
    }

    private void TimerOnTick(object? sender, EventArgs e)
    {
        CurrentOrder.Text = MacOSIntegration.GetOrderedIndex(this).ToString();
    }

    private void Button_OnClick(object? sender, RoutedEventArgs e)
    {
        Position += new PixelPoint(100, 100);
    }
}
