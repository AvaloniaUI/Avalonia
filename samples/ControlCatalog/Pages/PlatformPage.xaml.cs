using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using ControlCatalog.ViewModels;

namespace ControlCatalog.Pages
{
    public class PlatformPage : UserControl
    {
        private readonly TextBlock _textBlockLauncherStatus;

        public PlatformPage()
        {
            this.InitializeComponent();
            DataContext = new PlatformViewModel();

            _textBlockLauncherStatus = this.Find<TextBlock>("TextBlockLauncherStatus");
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }

        private async void LaunchUriClick(object? sender, RoutedEventArgs e)
        {
            var launcher = TopLevel.GetTopLevel(this)?.Launcher;
            if (launcher != null)
            {
                void LauncherOnUriLaunching(LauncherEventArgs<Uri> ea) => _textBlockLauncherStatus.Text = $"Launching {ea.Argument} ...";

                // Subscribe launcher event
                launcher.UriLaunching += LauncherOnUriLaunching;

                // Launch Avalonia UI website
                var success = await launcher.LaunchUriAsync(new Uri("https://avaloniaui.net/"));
                if (!success)
                    _textBlockLauncherStatus.Text = "Failed";

                // Unsubscribe launcher event
                launcher.UriLaunching -= LauncherOnUriLaunching;
            }
        }
    }
}
