using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;

namespace Avalonia.Headless.UnitTests;

public class InputTests
#if XUNIT
    : IDisposable
#endif
{
    private Window _window;
    private Application _setupApp;

#if NUNIT
    [SetUp]
    public void SetUp()
#elif XUNIT
    public InputTests()
#endif
    {
        _setupApp = Application.Current;
        Dispatcher.UIThread.VerifyAccess();
        _window = new Window
        {
            Width = 100,
            Height = 100
        };
    }
    
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Should_Click_Button_On_Window()
    {
        AssertHelper.Same(_setupApp, Application.Current);
        var buttonClicked = false;
        var button = new Button
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        button.Click += (_, _) => buttonClicked = true;
        
        _window.Content = button;
        _window.Show();

        _window.MouseDown(new Point(50, 50), MouseButton.Left);
        _window.MouseUp(new Point(50, 50), MouseButton.Left);

        AssertHelper.True(buttonClicked);
    }
    
#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Change_Window_Position()
    {
        var newWindowPosition = new PixelPoint(100, 150);
        _window.Position = newWindowPosition;
        _window.Show();
        AssertHelper.Equal(newWindowPosition, _window.Position);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Should_Click_Button_After_Explicit_RunJobs()
    {
        // Regression test for https://github.com/AvaloniaUI/Avalonia/issues/20309
        // Ensure that calling Dispatcher.UIThread.RunJobs() before MouseDown does not throw
        var button = new Button { Content = "Test content" };
        _window.Content = button;
        _window.Show();

        Dispatcher.UIThread.RunJobs();

        var clickCount = 0;
        button.Click += (_, _) => clickCount++;

        var point = new Point(button.Bounds.Width / 2, button.Bounds.Height / 2);
        var translatePoint = button.TranslatePoint(point, _window);

        // Move
        _window.MouseMove(translatePoint!.Value, RawInputModifiers.None);

        // Click
        _window.MouseDown(translatePoint.Value, MouseButton.Left, RawInputModifiers.None);
        _window.MouseUp(translatePoint.Value, MouseButton.Left, RawInputModifiers.None);

        AssertHelper.Equal(1, clickCount);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Touch_Contact_Raises_Touch_Pointer_Events()
    {
        var pressedCount = 0;
        var movedCount = 0;
        var releasedCount = 0;
        PointerType pressedPointerType = default;

        var border = new Border { Background = Brushes.Red };
        border.PointerPressed += (_, e) =>
        {
            pressedCount++;
            pressedPointerType = e.Pointer.Type;
        };
        border.PointerMoved += (_, _) => movedCount++;
        border.PointerReleased += (_, _) => releasedCount++;

        _window.Content = border;
        _window.Show();

        var touch = _window.TouchBegin(new Point(50, 50));
        _window.TouchMove(touch, new Point(60, 60));
        _window.TouchEnd(touch, new Point(60, 60));

        AssertHelper.Equal(1, pressedCount);
        AssertHelper.Equal(1, movedCount);
        AssertHelper.Equal(1, releasedCount);
        AssertHelper.Equal(PointerType.Touch, pressedPointerType);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Multiple_Touch_Contacts_Are_Distinct_Pointers()
    {
        var pointerIds = new HashSet<int>();

        var border = new Border { Background = Brushes.Red };
        border.PointerPressed += (_, e) => pointerIds.Add(e.Pointer.Id);

        _window.Content = border;
        _window.Show();

        var touch1 = _window.TouchBegin(new Point(30, 30));
        var touch2 = _window.TouchBegin(new Point(70, 70));
        _window.TouchEnd(touch1, new Point(30, 30));
        _window.TouchEnd(touch2, new Point(70, 70));

        AssertHelper.Equal(2, pointerIds.Count);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Disposing_Touch_Pointer_Cancels_Contact()
    {
        var captureLostCount = 0;
        var releasedCount = 0;

        var border = new Border { Background = Brushes.Red };
        border.PointerCaptureLost += (_, _) => captureLostCount++;
        border.PointerReleased += (_, _) => releasedCount++;

        _window.Content = border;
        _window.Show();

        using (_window.TouchBegin(new Point(50, 50)))
        {
        }

        AssertHelper.Equal(1, captureLostCount);
        AssertHelper.Equal(0, releasedCount);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Pen_Input_Reports_Pen_Type_And_Pressure()
    {
        PointerType pressedPointerType = default;
        float pressure = 0f;
        var releasedCount = 0;

        var border = new Border { Background = Brushes.Red };
        border.PointerPressed += (_, e) =>
        {
            pressedPointerType = e.Pointer.Type;
            pressure = e.GetCurrentPoint(border).Properties.Pressure;
        };
        border.PointerReleased += (_, _) => releasedCount++;

        _window.Content = border;
        _window.Show();

        using var pen = _window.PenBegin(new Point(40, 40));
        _window.PenDown(pen, new Point(50, 50), pressure: 0.75f);
        _window.PenMove(pen, new Point(60, 60), pressure: 0.75f);
        _window.PenUp(pen, new Point(60, 60));

        AssertHelper.Equal(PointerType.Pen, pressedPointerType);
        AssertHelper.Equal(0.75f, pressure);
        AssertHelper.Equal(1, releasedCount);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Pen_Hovers_Before_And_After_Press()
    {
        var movedCount = 0;
        var pressedCount = 0;

        var border = new Border { Background = Brushes.Red };
        border.PointerMoved += (_, _) => movedCount++;
        border.PointerPressed += (_, _) => pressedCount++;

        _window.Content = border;
        _window.Show();

        using var pen = _window.PenBegin(new Point(50, 50));
        _window.PenMove(pen, new Point(60, 60), pressure: 0f);

        AssertHelper.Equal(2, movedCount);
        AssertHelper.Equal(0, pressedCount);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Multiple_Pens_Are_Distinct_Pointers()
    {
        var pointerIds = new HashSet<int>();

        var border = new Border { Background = Brushes.Red };
        border.PointerPressed += (_, e) => pointerIds.Add(e.Pointer.Id);

        _window.Content = border;
        _window.Show();

        using var pen1 = _window.PenBegin(new Point(30, 30));
        using var pen2 = _window.PenBegin(new Point(70, 70));
        _window.PenDown(pen1, new Point(30, 30));
        _window.PenDown(pen2, new Point(70, 70));
        _window.PenUp(pen1, new Point(30, 30));
        _window.PenUp(pen2, new Point(70, 70));

        AssertHelper.Equal(2, pointerIds.Count);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Disposing_Pressed_Pen_Pointer_Cancels_Contact()
    {
        var captureLostCount = 0;
        var releasedCount = 0;

        var border = new Border { Background = Brushes.Red };
        border.PointerCaptureLost += (_, _) => captureLostCount++;
        border.PointerReleased += (_, _) => releasedCount++;

        _window.Content = border;
        _window.Show();

        using (var pen = _window.PenBegin(new Point(50, 50)))
        {
            _window.PenDown(pen, new Point(50, 50));
        }

        AssertHelper.Equal(1, captureLostCount);
        AssertHelper.Equal(0, releasedCount);
    }

#if NUNIT
    [TearDown]
    public void TearDown()
#elif XUNIT
    public void Dispose()
#endif
    {
        AssertHelper.Same(_setupApp, Application.Current);

        Dispatcher.UIThread.VerifyAccess();
        _window.Close();
    }
}
