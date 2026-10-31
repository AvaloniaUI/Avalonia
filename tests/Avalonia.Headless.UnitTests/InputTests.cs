using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.VisualTree;

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
    public void Pressed_Button_Should_Keep_IsPointerOver_While_Pointer_Moves_Within_It()
    {
        var text = new TextBlock { Text = "Text", Background = Brushes.Green, HorizontalAlignment = HorizontalAlignment.Center };
        var button = new Button
        {
            Padding = new Thickness(20),
            Content = text,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch
        };

        _window.Content = button;
        _window.Show();

        var onPadding = new Point(5, 5);
        var onText = text.TranslatePoint(new Point(text.Bounds.Width / 2, text.Bounds.Height / 2), _window)!.Value;

        _window.MouseDown(onPadding, MouseButton.Left);
        _window.MouseMove(onText, RawInputModifiers.LeftMouseButton);

        AssertHelper.True(button.IsPointerOver);
        AssertHelper.False(text.IsPointerOver);

        _window.MouseUp(onText, MouseButton.Left);

        AssertHelper.True(text.IsPointerOver);

        _window.MouseDown(onText, MouseButton.Left);
        _window.MouseMove(onPadding, RawInputModifiers.LeftMouseButton);

        AssertHelper.True(button.IsPointerOver);
        AssertHelper.False(text.IsPointerOver);

        _window.MouseUp(onPadding, MouseButton.Left);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Pressed_Pointer_Should_Not_Set_IsPointerOver_On_Other_Button()
    {
        var button = new Button { Width = 40, Height = 40 };
        var container = new Panel { Background = Brushes.Red, Children = { button } };

        _window.Content = container;
        _window.Show();

        var onContainer = new Point(5, 5);
        var onButton = new Point(50, 50);

        _window.MouseDown(onContainer, MouseButton.Left);
        _window.MouseMove(onButton, RawInputModifiers.LeftMouseButton);

        AssertHelper.True(container.IsPointerOver);
        AssertHelper.False(button.IsPointerOver);

        _window.MouseUp(onButton, MouseButton.Left);

        AssertHelper.True(button.IsPointerOver);
    }

#if NUNIT
    [AvaloniaTest]
#elif XUNIT
    [AvaloniaFact]
#endif
    public void Dragged_Slider_Should_Keep_IsPointerOver_While_Pointer_Is_Over_Its_Track()
    {
        var slider = new Slider
        {
            Maximum = 100,
            Value = 50,
            IsSnapToTickEnabled = true,
            TickFrequency = 50,
            VerticalAlignment = VerticalAlignment.Center
        };
        var outside = new Panel { Background = Brushes.Red, Children = { slider } };

        _window.Content = outside;
        _window.Show();

        var thumb = slider.GetVisualDescendants().OfType<Thumb>().First();
        var onThumb = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), _window)!.Value;
        var onTrack = onThumb + new Point(thumb.Bounds.Width, 0);
        var outsideSlider = new Point(onThumb.X, 1);

        _window.MouseDown(onThumb, MouseButton.Left);
        _window.MouseMove(onTrack, RawInputModifiers.LeftMouseButton);

        AssertHelper.True(slider.IsPointerOver);
        AssertHelper.False(thumb.IsPointerOver);

        _window.MouseMove(outsideSlider, RawInputModifiers.LeftMouseButton);

        AssertHelper.False(slider.IsPointerOver);
        AssertHelper.True(outside.IsPointerOver);

        _window.MouseUp(outsideSlider, MouseButton.Left);
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
