using System;
using Avalonia;

namespace SingleProjectSandbox;

internal static class Program
{
    [STAThread]
    static int Main(string[] args) =>
        BuildAvaloniaApp()
            .StartWithClassicDesktopLifetime(args);

    /// <summary>
    /// This method is needed for IDE previewer infrastructure
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => App.BuildAvaloniaApp().UsePlatformDetect();
}
