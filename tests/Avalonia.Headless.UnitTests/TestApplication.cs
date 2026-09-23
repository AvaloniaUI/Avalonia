using Avalonia.Themes.Simple;

namespace Avalonia.Headless.UnitTests;

public class TestApplication : Application
{
    public TestApplication()
    {
        Styles.Add(new SimpleTheme());
    }

    /// <summary>
    /// Enabled by the PerTest projects only, so that both mouse device modes are covered.
    /// </summary>
    public static bool UsesSharedMouseDevice =>
#if PERTEST
        true;
#else
        false;
#endif

    /// <summary>
    /// Shared with every headless window, so tests can flip a mode and restore it afterwards.
    /// </summary>
    public static AvaloniaHeadlessPlatformOptions Options { get; } = new()
    {
        UseHeadlessDrawing = false,
        OverlayPopups = false,
        UseSharedMouseDevice = UsesSharedMouseDevice
    };

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseHarfBuzz()
        .UseSkia()
        .UseHeadless(Options);
}
