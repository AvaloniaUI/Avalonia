using System;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class ServicesTests
{
#if NUNIT
    [AvaloniaTest, Timeout(10000)]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Can_Access_Screens()
    {
        var window = new Window();
        var screens = window.Screens;
        Assert.NotNull(screens);

        var currentScreenFromWindow = screens.ScreenFromWindow(window);
        var currentScreenFromVisual = screens.ScreenFromVisual(window);

        Assert.True(ReferenceEquals(currentScreenFromWindow, currentScreenFromVisual));
    }
}
