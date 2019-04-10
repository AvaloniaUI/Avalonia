using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;
using System.Threading.Tasks;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        private readonly NotificationManager _notificationManager = new NotificationManager();

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            //Renderer.DrawFps = true;
            //Renderer.DrawDirtyRects = Renderer.DrawFps = true;

            Dispatcher.UIThread.InvokeAsync(async () =>
            {
                await Task.Delay(5000);

                _notificationManager.Show(new NotificationContent { Message = "Test1", Type = NotificationType.Information }, "Main");

                await Task.Delay(500);
                _notificationManager.Show(new NotificationContent { Message = "Test2", Type = NotificationType.Error }, "Main");

                await Task.Delay(500);
                _notificationManager.Show(new NotificationContent { Message = "Test3", Type = NotificationType.Warning }, "Main");

                await Task.Delay(500);
                _notificationManager.Show(new NotificationContent { Message = "Test4", Type = NotificationType.Success }, "Main");

                await Task.Delay(500);
                _notificationManager.Show("Test5", "Main");

            });
        }

        private void InitializeComponent()
        {
            // TODO: iOS does not support dynamically loading assemblies
            // so we must refer to this resource DLL statically. For
            // now I am doing that here. But we need a better solution!!
            var theme = new Avalonia.Themes.Default.DefaultTheme();
            theme.TryGetResource("Button", out _);
            AvaloniaXamlLoader.Load(this);
        }
    }
}
