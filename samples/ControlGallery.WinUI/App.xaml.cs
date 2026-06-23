using global::Avalonia;
using global::Avalonia.Controls;
using global::Avalonia.Controls.ApplicationLifetimes;
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

        internal static SingleViewLifetime Lifetime { get; } = new();

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
                .SetupWithLifetime(Lifetime);

            _window = new MainWindow();
            _window.Activate();
        }
    }

    internal sealed class SingleViewLifetime : ISingleViewApplicationLifetime
    {
        public Control? MainView { get; set; }
    }
}
