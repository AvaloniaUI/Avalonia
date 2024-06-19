using System.Windows.Input;

using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

using ReactiveUI;

namespace IntegrationTestApp
{
    public class App : Application
    {
        public App()
        {
            ShowWindowCommand = ReactiveCommand.Create(ShowTestWindow);
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

        void ShowTestWindow()
        {
            var window = new ShowWindowTest();

            window.Show();
        }
    }
}
