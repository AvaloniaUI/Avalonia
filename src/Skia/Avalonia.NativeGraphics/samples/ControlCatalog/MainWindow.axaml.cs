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
        private WindowNotificationManager _notificationArea;
        private NativeMenu _recentMenu;

        public MainWindow()
        {
            this.InitializeComponent();
            this.AttachDevTools();
            //Renderer.DrawFps = true;
            //Renderer.DrawDirtyRects = Renderer.DrawFps = true;

            _notificationArea = new WindowNotificationManager(this)
            {
                Position = NotificationPosition.TopRight,
                MaxItems = 3
            };

            DataContext = new MainWindowViewModel(_notificationArea);
            _recentMenu = ((NativeMenu.GetMenu(this).Items[0] as NativeMenuItem).Menu.Items[2] as NativeMenuItem).Menu;

            var mainMenu = this.FindControl<Menu>("MainMenu");
            mainMenu.AttachedToVisualTree += MenuAttached;
        }

        public static string MenuQuitHeader => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ? "Quit Avalonia" : "E_xit";

        public static KeyGesture MenuQuitGesture => RuntimeInformation.IsOSPlatform(OSPlatform.OSX) ?
            new KeyGesture(Key.Q, KeyModifiers.Meta) :
            new KeyGesture(Key.F4, KeyModifiers.Alt);

        public void MenuAttached(object sender, VisualTreeAttachmentEventArgs e)
        {
            if (NativeMenu.GetIsNativeMenuExported(this) && sender is Menu mainMenu)
            {
                mainMenu.IsVisible = false;
            }
        }

        public void OnOpenClicked(object sender, EventArgs args)
        {
            _recentMenu.Items.Insert(0, new NativeMenuItem("Item " + (_recentMenu.Items.Count + 1)));
        }

        public void OnCloseClicked(object sender, EventArgs args)
        {
            Close();
        }

        private void InitializeComponent()
        {
            // TODO: iOS does not support dynamically loading assemblies
            // so we must refer to this resource DLL statically. For
            // now I am doing that here. But we need a better solution!!
            // Note, theme swiching probably will not work in runtime for iOS.
            if (Application.Current.Styles.Contains(App.FluentDark)
                || Application.Current.Styles.Contains(App.FluentLight))
            {
                var theme = new Avalonia.Themes.Fluent.Controls.FluentControls();
                theme.TryGetResource("Button", out _);
            }
            else
            {
                var theme = new Avalonia.Themes.Default.DefaultTheme();
                theme.TryGetResource("Button", out _);
            }
            AvaloniaXamlLoader.Load(this);
        }
    }
}
