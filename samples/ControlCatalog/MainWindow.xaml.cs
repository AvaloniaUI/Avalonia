using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            //Renderer.DrawDirtyRects = Renderer.DrawFps = true;
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
