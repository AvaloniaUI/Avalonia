using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Controls.Primitives;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using ControlCatalog.ViewModels;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls.Notifications.Native;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            //Renderer.DrawFps = true;
            //Renderer.DrawDirtyRects = Renderer.DrawFps = true;

            var notificationArea = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3
            };
            var nativeNotification = AvaloniaLocator.Current.GetService<INativeNotificationManager>();

            DataContext = new MainWindowViewModel(notificationArea, nativeNotification);
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
