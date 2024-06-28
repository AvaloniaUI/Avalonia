using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using MiniMvvm;

namespace IntegrationTestApp
{
    public class App : Application
    {
        public App()
        {
            ShowWindowCommand = MiniCommand.Create(() =>
            {
                var window = new Window() { Title = "TrayIcon demo window" };
                window.Show();
            });
            DataContext = this;
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
            {
                desktop.MainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public ICommand ShowWindowCommand { get; }
    }
}
