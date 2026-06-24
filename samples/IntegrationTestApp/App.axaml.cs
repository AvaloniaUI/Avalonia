using System.Linq;
using System.Windows.Input;

using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.LogicalTree;
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
                // This is for the "Show Main Window" dock menu item in the test.
                // It doesn't actually show the main window, but sets the checkbox to true in the page.
                var checkbox = _mainWindow!.GetLogicalDescendants().OfType<CheckBox>().FirstOrDefault(x => x.Name == name); 
                if (checkbox != null) checkbox.IsChecked = true;
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
            var dockMenu = NativeDock.GetMenu(this);
            if (dockMenu is not null)
            {
                dockMenu.Items.Insert(0, new NativeMenuItem(header));
            }
        }

        public int GetDockMenuItemCount()
        {
            var dockMenu = NativeDock.GetMenu(this);
            return dockMenu?.Items.Count ?? 0;
        }
    }
}
