using Avalonia.Headless;
using Avalonia.Themes.Simple;

namespace Avalonia.UnitTests;

public class HeadlessApplication : Application
{
    public HeadlessApplication()
    {
        Styles.Add(new SimpleTheme());
    }
    
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<HeadlessApplication>()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = true });
}

public class HeadlessSkiaApplication : Application
{
    public HeadlessSkiaApplication()
    {
        Styles.Add(new SimpleTheme());
    }

    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder
            .Configure<HeadlessSkiaApplication>()
            .UseSkia()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
}
