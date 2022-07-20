using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Win32.WinRT.Composition;

namespace Sandbox
{
    public class MainWindow : Window
    {
        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
        private void MyButton_OnClick(object? sender, RoutedEventArgs e)
        {
            var topMenu = NativeMenu.GetMenu(this);

            BuildNativeTopMenus(topMenu);

        }
        private void MyButton_OnClick2(object? sender, RoutedEventArgs e)
        { 

        }

        private void ForGCCollectButton_OnClick(object? sender, RoutedEventArgs e)
        {
            GC.Collect();
        }


        public void BuildNativeTopMenus(NativeMenu nativeMenu)
        {
            if (nativeMenu != null)
            {
                var topMenus = new string[] { "TopMenu1", "TopMenu2" };
                nativeMenu.Items.Clear(); // memory should be cleared when clearing the old menuItems
                foreach (var topMenu in topMenus)
                {
                    var menuItem = new NativeMenuItem(topMenu);
                    var menu = new NativeMenu();
                    menuItem.Menu = menu;
                    nativeMenu.Add(menuItem);
                }
            }
        }

    }
}
