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
        private MainWindow? _mainWindow;

        public App()
        {
            TrayIconCommand = MiniCommand.Create<string>(name =>
            {
                _mainWindow!.Get<CheckBox>(name).IsChecked = true;
            });
            DockMenuCommand = MiniCommand.Create<string>(name =>
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
        public ICommand DockMenuCommand { get; }

        public void AddDockMenuItem(string header)
        {
            var dockMenu = NativeMenu.GetDockMenu(this);
            if (dockMenu is not null)
            {
                dockMenu.Items.Insert(0, new NativeMenuItem(header));
            }
        }

        public int GetDockMenuItemCount()
        {
            var dockMenu = NativeMenu.GetDockMenu(this);
            return dockMenu?.Items.Count ?? 0;
        }
    }
}
