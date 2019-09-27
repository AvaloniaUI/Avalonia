using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace ControlCatalog
{
    public class App : Application
    {
        private NativeMenu _recentMenu;

        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);

            Name = "Avalonia";

            _recentMenu = (NativeMenu.GetMenu(this).Items[1] as NativeMenuItem).Menu;
        }

        public void OnOpenClicked(object sender, EventArgs args)
        {
            _recentMenu.Items.Insert(0, new NativeMenuItem("Item " + (_recentMenu.Items.Count + 1)));
        }

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
