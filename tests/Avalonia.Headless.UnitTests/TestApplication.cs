using Avalonia.Headless.XUnit;
using Avalonia.Headless.XUnit.Tests;
using Avalonia.Themes.Simple;
using Xunit;

[assembly: AvaloniaTestFramework(typeof(TestApplication))]
[assembly: CollectionBehavior(DisableTestParallelization = true)]

namespace Avalonia.Headless.XUnit.Tests;

public class TestApplication : Application
{
    public TestApplication()
    {
        Styles.Add(new SimpleTheme());
    }

    public static AppBuilder BuildAvaloniaApp() => AppBuilder.Configure<TestApplication>()
        .UseSkia()
        .UseHeadless(new AvaloniaHeadlessPlatformOptions
        {
            UseHeadlessDrawing = false
        });
}
