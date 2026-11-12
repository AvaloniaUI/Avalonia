using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Themes.Fluent;
using Avalonia.Threading;

namespace ImpellerSandbox;

public class App : Application
{
    public override void Initialize() => Styles.Add(new FluentTheme());

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is ISingleViewApplicationLifetime singleView)
            singleView.MainView = new SandboxView();
        else if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
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
