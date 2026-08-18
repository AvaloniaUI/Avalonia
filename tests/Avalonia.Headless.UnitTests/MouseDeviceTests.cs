using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class MouseDeviceTests
{
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Pointer_Is_Shared_Between_Windows_When_Requested()
    {
        var firstWindow = CreateWindow(out var firstTarget);
        var secondWindow = CreateWindow(out var secondTarget);

        IPointer firstPointer = null, secondPointer = null;
        firstTarget.PointerPressed += (_, e) => firstPointer = e.Pointer;
        secondTarget.PointerPressed += (_, e) => secondPointer = e.Pointer;

        Click(firstWindow);
        Click(secondWindow);

        AssertHelper.NotNull(firstPointer);
        AssertHelper.NotNull(secondPointer);

        if (TestApplication.UsesSharedMouseDevice)
            AssertHelper.Same(firstPointer, secondPointer);
        else
            AssertHelper.NotSame(firstPointer, secondPointer);

        firstWindow.Close();
        secondWindow.Close();
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Pointer_Capture_Crosses_TopLevels_When_Device_Is_Shared()
    {
        var popupChild = new Border { Width = 80, Height = 30, Background = Brushes.Blue };
        var target = new Border { Background = Brushes.Red };
        var popup = new Popup { PlacementTarget = target, Child = popupChild };
        var window = new Window
        {
            Width = 100,
            Height = 100,
            Content = new Panel { Children = { target, popup } }
        };
        window.Show();
        Dispatcher.UIThread.RunJobs();

        popup.Open();
        Dispatcher.UIThread.RunJobs();

        object moveTarget = null;
        target.PointerMoved += (s, _) => moveTarget = s;
        popupChild.PointerMoved += (s, _) => moveTarget = s;

        // Pressing captures the pointer implicitly on the window's border.
        window.MouseDown(new Point(50, 50), MouseButton.Left);
        window.GetOpenPopups()[0].MouseMove(new Point(40, 15));

        AssertHelper.Same(TestApplication.UsesSharedMouseDevice ? target : popupChild, moveTarget);

        window.MouseUp(new Point(50, 50), MouseButton.Left);
        window.Close();
    }

    private static Window CreateWindow(out Border target)
    {
        target = new Border { Background = Brushes.Red };
        var window = new Window { Width = 100, Height = 100, Content = target };
        window.Show();
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static void Click(Window window)
    {
        window.MouseDown(new Point(50, 50), MouseButton.Left);
        window.MouseUp(new Point(50, 50), MouseButton.Left);
    }
}
