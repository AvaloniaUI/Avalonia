using Avalonia;
using Avalonia.Markup.Xaml;
using Avalonia.ReactiveUI;

namespace RenderDemo
{
    public class App : Application
    {
        public override void Initialize()
        {
            AvaloniaXamlLoader.Load(this);
        }

        // TODO: Make this work with GTK/Skia/Cairo depending on command-line args
        // again.
        static void Main(string[] args) => BuildAvaloniaApp().Start<MainWindow>();

        // App configuration, used by the entry point and previewer
        static AppBuilder BuildAvaloniaApp()
           => AppBuilder.Configure<App>()
                .UsePlatformDetect()
                .UseReactiveUI()
                .LogToDebug();
    }
}
