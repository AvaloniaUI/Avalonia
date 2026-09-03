using System;
using Avalonia.Controls.Chrome;
using Avalonia.Controls.Platform;
using Avalonia.Controls.Templates;
using Avalonia.Input;
using Avalonia.Input.Raw;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.Templates;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.UnitTests;
using Avalonia.VisualTree;
using Moq;
using Xunit;

namespace Avalonia.Controls.UnitTests;

public sealed class PresentationSourceTests : ScopedTestBase
{
    [Fact]
    public void Closing_Should_Detach_Platform_Input_Handler()
    {
        using var app = UnitTestApplication.Start(TestServices.StyledWindow);
        var windowImpl = MockWindowingPlatform.CreateWindowMock();
        var window = new Window(windowImpl.Object);

        Assert.NotNull(windowImpl.Object.Input);

        windowImpl.Object.Closed!();

        Assert.Null(windowImpl.Object.Input);
    }

    [Fact]
    public void ChromeHitTest_Prefers_Overlay_Over_Content()
    {
        var overlay = new Border
        {
            Background = Brushes.Red,
            [WindowDecorationProperties.ElementRoleProperty] = WindowDecorationsElementRole.TitleBar
        };

        var content = new Border
        {
            Background = Brushes.Blue
        };

        DoChromeHitTest(
            underlay: null,
            content: content,
            overlay: overlay,
            expectedChromeVisual: overlay,
            expectedRole: WindowDecorationsElementRole.TitleBar);
    }

    [Fact]
    public void ChromeHitTest_Prefers_Content_Over_Underlay()
    {
        var underlay = new Border
        {
            Background = Brushes.Red,
            [WindowDecorationProperties.ElementRoleProperty] = WindowDecorationsElementRole.TitleBar
        };

        var content = new Border
        {
            Background = Brushes.Blue
        };

        DoChromeHitTest(
            underlay: underlay,
            content: content,
            overlay: null,
            expectedChromeVisual: content,
            expectedRole: null);
    }

    private static void DoChromeHitTest(
        Control? underlay,
        Control? content,
        Control? overlay,
        Visual? expectedChromeVisual,
        WindowDecorationsElementRole? expectedRole)
    {
        const double width = 100;
        const double height = 100;

        if (underlay is not null)
        {
            underlay.Width = width;
            underlay.Height = height;
        }

        if (content is not null)
        {
            content.Width = width;
            content.Height = height;
        }

        if (overlay is not null)
        {
            overlay.Width = width;
            overlay.Height = height;
        }

        using var app = UnitTestApplication.Start(TestServices.StyledWindow);

        var decorations = new WindowDrawnDecorationsContent
        {
            Underlay = underlay,
            Overlay = overlay
        };

        Application.Current!.Resources.Add(typeof(WindowDrawnDecorations), CreateDecorationsTheme(decorations));

        var renderTimer = new CompositorTestServices.ManualRenderTimer();
        var compositor = RendererMocks.CreateDummyCompositor(renderTimer);
        var windowImpl = MockWindowingPlatform.CreateWindowMock(width, height, compositor);
        windowImpl.Setup(w => w.IsClientAreaExtendedToDecorations).Returns(true);
        windowImpl.Setup(w => w.RequestedDrawnDecorations).Returns(PlatformRequestedDrawnDecoration.TitleBar);
        windowImpl.Setup(w => w.NeedsManagedDecorations).Returns(true);

        var window = new Window(windowImpl.Object)
        {
            WindowDecorations = WindowDecorations.Full,
            ExtendClientAreaToDecorationsHint = true,
            Content = content
        };

        window.Show();
        Render(renderTimer);

        var hitTestPoint = new Point(width / 2, height / 2);

        var clientVisual = window.GetVisualAt(hitTestPoint);
        Assert.Same(window.Content, clientVisual);

        var chromeVisual = window.PresentationSource.RootVisual.GetVisualAt(hitTestPoint);
        Assert.Same(expectedChromeVisual, chromeVisual);

        var chromeRole = ((IInputRoot)window.PresentationSource).HitTestChromeElement(hitTestPoint);
        Assert.Equal(expectedRole, chromeRole);
    }

    private static ControlTheme CreateDecorationsTheme(WindowDrawnDecorationsContent content)
    {
        var template = new WindowDrawnDecorationsTemplate
        {
            Content = (IServiceProvider? _) => new TemplateResult<WindowDrawnDecorationsContent>(content, new NameScope())
        };

        return new ControlTheme(typeof(WindowDrawnDecorations))
        {
            Setters =
            {
                new Setter(WindowDrawnDecorations.TemplateProperty, template)
            }
        };
    }

    [Fact]
    public void Cursor_Should_Follow_Captured_Element()
    {
        using var app = UnitTestApplication.Start(
            TestServices.StyledWindow.With(inputManager: new InputManager()));

        var captured = new Border
        {
            Background = Brushes.Red,
            Width = 20,
            Cursor = new Cursor(StandardCursorType.SizeWestEast)
        };

        var other1 = new Border
        {
            Background = Brushes.Blue,
            Width = 100,
            Cursor = new Cursor(StandardCursorType.Ibeam)
        };

        var other2 = new Border
        {
            Background = Brushes.Blue,
            Width = 100,
            Cursor = new Cursor(StandardCursorType.Cross)
        };

        ICursorImpl? currentCursor = null;
        var renderTimer = new CompositorTestServices.ManualRenderTimer();
        var compositor = RendererMocks.CreateDummyCompositor(renderTimer);
        var windowImpl = MockWindowingPlatform.CreateWindowMock(200, 100, compositor);
        windowImpl
            .Setup(w => w.SetCursor(It.IsAny<ICursorImpl?>()))
            .Callback<ICursorImpl?>(cursor => currentCursor = cursor);

        var window = new Window(windowImpl.Object)
        {
            Content = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children = { captured, other1, other2 }
            }
        };

        IPointer? pointer = null;
        captured.PointerPressed += (_, e) =>
        {
            e.Pointer.Capture(captured);
            pointer = e.Pointer;
        };

        window.Show();
        Render(renderTimer);

        var mouse = new MouseDevice();
        var root = window.PresentationSource;

        // Press inside the first border: the pointer becomes captured and the cursor is its own.
        windowImpl.Object.Input!(new RawPointerEventArgs(
            mouse, 1, root, RawPointerEventType.LeftButtonDown, new Point(10, 50),
            RawInputModifiers.LeftMouseButton));

        Assert.NotNull(pointer);
        Assert.Same(captured, pointer.Captured);
        Assert.Same(captured.Cursor!.PlatformImpl, currentCursor);
        var cursorWhileCaptured = currentCursor;

        // Drag over the other border. With the pointer still captured by the first border,
        // PresentationSource.PointerOverElement changes (it becomes null), but the displayed
        // cursor must keep coming from the captured element rather than following the new
        // PointerOverElement.
        windowImpl.Object.Input!(new RawPointerEventArgs(
            mouse, 2, root, RawPointerEventType.Move, new Point(70, 50),
            RawInputModifiers.LeftMouseButton));

        Assert.Same(captured, pointer.Captured);
        Assert.Same(cursorWhileCaptured, currentCursor);

        // Changing the captured element's cursor should still work.
        var newCursor = new Cursor(StandardCursorType.Hand);
        captured.Cursor = newCursor;
        Assert.Same(newCursor.PlatformImpl, currentCursor);

        // Changing the capture explicitly should update the cursor.
        pointer.Capture(other1);
        Assert.Same(other1.Cursor!.PlatformImpl, currentCursor);

        // Move the pointer to an unrelated element and release the capture:
        // it should reset the cursor to match that new element.
        ((IInputRoot)root).PointerOverElement = other2;

        windowImpl.Object.Input!(new RawPointerEventArgs(
            mouse, 2, root, RawPointerEventType.Move, new Point(120, 50),
            RawInputModifiers.LeftMouseButton));

        pointer.Capture(null);
        Assert.Same(other2.Cursor!.PlatformImpl, currentCursor);
    }

    private static void Render(CompositorTestServices.ManualRenderTimer renderTimer)
    {
        Dispatcher.CurrentDispatcher.RunJobs(null, TestContext.Current.CancellationToken);
        renderTimer.TriggerTick();
        Dispatcher.CurrentDispatcher.RunJobs(null, TestContext.Current.CancellationToken);
    }
}
