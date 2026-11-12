using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace ImpellerSandbox;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // RENDERER=skia renders the same window with Skia for comparison.
    public static AppBuilder BuildAvaloniaApp()
    {
        if (Environment.GetEnvironmentVariable("RENDERER") is null)
            Environment.SetEnvironmentVariable("RENDERER", "impeller");

        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .UseImpellerIfRequested()
            .WithInterFont()
            .LogToTrace();
    }
}

internal class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();

            // IMPELLER_SMOKE=1 renders for a moment and exits.
            if (Environment.GetEnvironmentVariable("IMPELLER_SMOKE") == "1")
            {
                desktop.MainWindow.Topmost = true;
                var window = desktop.MainWindow;
                DispatcherTimer.RunOnce(() => window.Width += 120, TimeSpan.FromSeconds(1.5));
                DispatcherTimer.RunOnce(() =>
                {
                    Console.WriteLine("IMPELLER_SMOKE: rendered, exiting.");
                    desktop.Shutdown();
                }, TimeSpan.FromSeconds(4));
            }
        }

        base.OnFrameworkInitializationCompleted();
    }
}
