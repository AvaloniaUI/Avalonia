using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;

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

            var window = new MainWindow(false);

            window.Owner = this;

            Dispatcher.UIThread.Post(() =>
            {
                window.ShowDialog();
            });
        }

        public MainWindow (bool childWindow)
        {
            this.InitializeComponent();
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
