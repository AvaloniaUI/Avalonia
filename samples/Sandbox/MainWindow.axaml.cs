using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Win32.WinRT.Composition;

#nullable enable

namespace Sandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private void Button_OnClick(object? sender, RoutedEventArgs e)
        {
            var i = 20;

            void Go()
            {
                var window = ShowWindow();
                window.Opened += async delegate (object? o, EventArgs args)
                {
                    await Task.Delay(2000);
                    window.Close();
                    if (i-- > 0)
                    {
                        Go();
                    }
                };
            }

            Go();
        }

        private Window ShowWindow()
        {
            var window = new Window();
            var heights = new[] { 491, 494, 554 };
            var stack = new StackPanel();
            stack.Orientation = Orientation.Horizontal;

            for (var i = 0; i < heights.Length; i++)
            {
                var border = new Border();
                border.Background = new SolidColorBrush(Color.FromRgb(200, 200, 200));
                border.Width = 100;
                border.Child = new TextBlock() { Text = i.ToString() };
                border.Height = heights[i] - 5;
                border.VerticalAlignment = VerticalAlignment.Top;
                stack.Children.Add(border);
            }

            stack.Height = 1;
            window.Content = stack;
            window.VerticalContentAlignment = VerticalAlignment.Top;
            window.SizeToContent = SizeToContent.Height;
            window.CanResize = false;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            void SetHeight(int i)
            {
                Dispatcher.UIThread.InvokeAsync(() =>
                {
                    stack.Height = heights[i];
                });
            }

            async Task WhenLayoutReady()
            {
                await Task.Delay(176);
                SetHeight(0);
            }

            WhenLayoutReady().ContinueWith(async _ => {
                Task.Run(async () =>
                {
                    await Task.Delay(22);
                    SetHeight(1);
                    await Task.Delay(20);
                    SetHeight(2);
                });
                Dispatcher.UIThread.InvokeAsync(() => { window.Show(); });
            }, TaskContinuationOptions.ExecuteSynchronously);
            return window;
        }
    }
}
