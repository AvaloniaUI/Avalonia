using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Simple;

namespace Avalonia.Wayland.Embedding.Tests;

/// <summary>
/// Headless host application for the embedding tests. Uses real Skia drawing (not the no-op headless
/// backend) so <c>CaptureRenderedFrame</c> returns the surface bitmaps the compositor hands to the UI, and
/// so the <c>MediaContext.BeforeRenderCore</c> drain runs.
/// </summary>
public class TestApplication : Application
{
    public TestApplication()
    {
        Styles.Add(new SimpleTheme());
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseHarfBuzz()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        });
}
