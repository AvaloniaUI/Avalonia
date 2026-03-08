using Avalonia;

namespace Sandbox
{
    public class Program
    {
        static void Main(string[] args) => BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

        public static AppBuilder BuildAvaloniaApp() =>
            AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .WithDeveloperTools()
                .LogToTrace();
    }

    public class MainWindowViewModel
    {
        public Tab1ViewModel Tab1 { get; set; } = new Tab1ViewModel();
    }

    public class Tab1ViewModel
    {
        public string Name { get; set; } = "Tab 1 message here";
    }
}
