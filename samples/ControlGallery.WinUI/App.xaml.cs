using global::Avalonia;
using global::Avalonia.Skia;
using global::Avalonia.Win32;
using WinUIApplication = Microsoft.UI.Xaml.Application;
using LaunchActivatedEventArgs = Microsoft.UI.Xaml.LaunchActivatedEventArgs;
using Window = Microsoft.UI.Xaml.Window;

namespace ControlGallery.WinUI
{
    public partial class App : WinUIApplication
    {
        private Window? _window;

        public App()
        {
            InitializeComponent();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            AppBuilder.Configure<ControlCatalog.App>()
                .UseWin32()
                .UseSkia()
                .UseHarfBuzz()
                .SetupWithoutStarting();

            _window = new MainWindow();
            _window.Activate();
        }
    }
}
