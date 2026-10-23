using System;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Sandbox
{
    public partial class MainWindow : Window
    {
        private readonly Stopwatch _clock = Stopwatch.StartNew();

        public MainWindow()
        {
            InitializeComponent();

            DispatcherTimer.Run(() =>
            {
                counter.Text = $"{_clock.Elapsed.TotalSeconds:F2}s";
                return true;
            }, TimeSpan.FromMilliseconds(50));
        }

        // Shows a window, then posts work that starts a nested loop, as a docking library does when
        // it floats a window and starts dragging it.
        private void ShowWindowThenNestedLoop(object? sender, RoutedEventArgs e)
        {
            new Window
            {
                Title = "New window",
                Width = 400,
                Height = 250,
                Position = Position + new PixelPoint(650, 50),
                Background = Brushes.LightGreen,
                Content = new TextBlock
                {
                    Text = "New window content",
                    FontSize = 28,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            }.Show();

            Dispatcher.UIThread.Post(() =>
            {
                var frame = new DispatcherFrame();
                DispatcherTimer.RunOnce(() => frame.Continue = false, TimeSpan.FromSeconds(3));
                Dispatcher.UIThread.PushFrame(frame);
            }, DispatcherPriority.Normal);
        }
    }
}
