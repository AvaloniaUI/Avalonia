global using Xunit;
global using Avalonia.Headless.XUnit;
using Avalonia;
using Avalonia.Headless;
using Avalonia.Themes.Fluent2.UnitTests;

[assembly: AvaloniaTestApplication(typeof(TestApplication))]

namespace Avalonia.Themes.Fluent2.UnitTests;

public class TestApplication : Application
{
    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseHarfBuzz()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        });
}
