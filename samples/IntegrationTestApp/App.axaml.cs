using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using MiniMvvm;

namespace IntegrationTestApp
{
    public class App : Application
    {
        private MainWindow? _mainWindow;

        public App()
        {
            TrayIconCommand = MiniCommand.Create<string>(name =>
            {
                _mainWindow!.Get<CheckBox>(name).IsChecked = true;
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
                desktop.MainWindow = _mainWindow = new MainWindow();
            }

            base.OnFrameworkInitializationCompleted();
        }

        public ICommand TrayIconCommand { get; }
    }
}
