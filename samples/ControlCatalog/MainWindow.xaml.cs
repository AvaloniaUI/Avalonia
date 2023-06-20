using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Notifications;
using Avalonia.Input;
using Avalonia.Markup.Xaml;
using ControlCatalog.ViewModels;

namespace ControlCatalog
{
    public class MainWindow : Window
    {
        private NativeMenu? _recentMenu;

        public MainWindow()
        {
            this.InitializeComponent();

            _recentMenu = ((NativeMenu.GetMenu(this)?.Items[0] as NativeMenuItem)?.Menu?.Items[2] as NativeMenuItem)?.Menu;
        }

        public static string MenuQuitHeader => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Quit Avalonia" : "E_xit";

        public static KeyGesture MenuQuitGesture => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            new KeyGesture(Key.Q, KeyModifiers.Meta) :
            new KeyGesture(Key.F4, KeyModifiers.Alt);

        public void OnOpenClicked(object sender, EventArgs args)
        {
            _recentMenu?.Items.Insert(0, new NativeMenuItem("Item " + (_recentMenu.Items.Count + 1)));
        }

        public void OnCloseClicked(object sender, EventArgs args)
        {
            Close();
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);
        }
    }
}
