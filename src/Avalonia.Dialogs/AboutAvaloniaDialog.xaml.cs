using System;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace Avalonia.Dialogs
{
    public class AboutAvaloniaDialog : Window
    {
        private static readonly Version s_version = typeof(AboutAvaloniaDialog).Assembly.GetName().Version!;

        public static string Version { get; } = $@"v{s_version.ToString(2)}";

        public static bool IsDevelopmentBuild { get; } = s_version.Revision == 999;

        public static string Copyright { get; } = $"© {DateTime.Now.Year} The Avalonia Project";

        public AboutAvaloniaDialog()
        {
            AvaloniaXamlLoader.Load(this);
            DataContext = this;
        }

        private async void Button_OnClick(object sender, RoutedEventArgs e)
        {
            var url = new Uri("https://www.avaloniaui.net/");
            await Launcher.LaunchUriAsync(url);
        }
    }
}
