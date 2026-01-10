using Avalonia.Controls;

namespace Avalonia.Headless.UnitTests;

public class ServicesTests
{
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Can_Access_Screens()
    {
        var window = new Window();
        var screens = window.Screens;
        AssertHelper.NotNull(screens);

        var currentScreenFromWindow = screens.ScreenFromWindow(window);
        var currentScreenFromVisual = screens.ScreenFromVisual(window);

        AssertHelper.Same(currentScreenFromWindow, currentScreenFromVisual);
    }
}
