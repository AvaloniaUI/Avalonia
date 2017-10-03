using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using System;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        private bool set = false;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            //Renderer.DrawFps = true;
            //Renderer.DrawDirtyRects = Renderer.DrawFps = true;

            var timer = new DispatcherTimer();

            LoadTheme(ColorTheme.VisualStudioLight);

            timer.Interval = TimeSpan.FromSeconds(5);
            timer.Tick += (sender, e) =>
            {
                if (set)
                {
                    LoadTheme(ColorTheme.VisualStudioLight);
                }
                else
                {
                    LoadTheme(ColorTheme.VisualStudioDark);
                }

                set = !set;
            };

            timer.Start();
        }

        public void LoadTheme(ColorTheme theme)
        {
            Resources["ThemeBackgroundBrush"] = theme.Background;
            Resources["ThemeControlDarkBrush"] = theme.ControlDark;
            Resources["ThemeControlMidBrush"] = theme.ControlMid;
            Resources["ThemeForegroundBrush"] = theme.Foreground;
            Resources["ThemeBorderDarkBrush"] = theme.BorderDark;
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
