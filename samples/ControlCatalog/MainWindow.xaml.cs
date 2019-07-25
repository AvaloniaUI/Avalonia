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

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        private WindowNotificationManager _notificationArea;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            //Renderer.DrawFps = true;
            //Renderer.DrawDirtyRects = Renderer.DrawFps = true;

            _notificationArea = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3
            };

            DataContext = new MainWindowViewModel(_notificationArea);

            Dispatcher.UIThread.Post(() =>
            {
                new OpenFileDialog()
                {
                    Filters = new List<FileDialogFilter>
                {
                    new FileDialogFilter {Name = "All files", Extensions = {"*"}},
                    new FileDialogFilter {Name = "Image files", Extensions = {"jpg", "png", "gif"}}
                },
                    Directory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop),
                    Title = "My dialog",
                    InitialFileName = "config.local.json",
                    AllowMultiple = true
                }.ShowAsync(this);
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
