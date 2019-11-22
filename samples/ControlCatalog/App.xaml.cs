using System;
using System.Reactive;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using ReactiveUI;

namespace ControlCatalog
{
    public class App : Application
    {
        public App()
        {
            DataContext = this;

            AboutCommand = ReactiveCommand.Create(() =>
            {

            });
        }

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        public ReactiveCommand<Unit, Unit> AboutCommand { get; }

        public override void OnFrameworkInitializationCompleted()
        {
            if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktopLifetime)
                desktopLifetime.MainWindow = new MainWindow();
            else if (ApplicationLifetime is ISingleViewApplicationLifetime singleViewLifetime)
                singleViewLifetime.MainView = new MainView();
            
            base.OnFrameworkInitializationCompleted();
        }
    }
}
