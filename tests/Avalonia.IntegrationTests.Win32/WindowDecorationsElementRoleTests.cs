using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class WindowDecorationsElementRoleTests : IDisposable
{
    private Window? _window;

    private Window Window
    {
        get
        {
            Assert.NotNull(_window);
            return _window;
        }
    }

    // Going fullscreen from Maximized keeps the maximized window placement, so both windowed states are covered.
    [Theory]
    [InlineData(WindowState.Normal, WindowDecorations.Full, WindowDecorationsElementRole.TitleBar, (int)HitTestValues.HTCAPTION)]
    [InlineData(WindowState.Normal, WindowDecorations.BorderOnly, WindowDecorationsElementRole.TitleBar, (int)HitTestValues.HTCAPTION)]
    [InlineData(WindowState.Normal, WindowDecorations.None, WindowDecorationsElementRole.TitleBar, (int)HitTestValues.HTCAPTION)]
    [InlineData(WindowState.Normal, WindowDecorations.BorderOnly, WindowDecorationsElementRole.ResizeE, (int)HitTestValues.HTRIGHT)]
    [InlineData(WindowState.Normal, WindowDecorations.BorderOnly, WindowDecorationsElementRole.ResizeSE, (int)HitTestValues.HTBOTTOMRIGHT)]
    [InlineData(WindowState.Maximized, WindowDecorations.Full, WindowDecorationsElementRole.TitleBar, (int)HitTestValues.HTCAPTION)]
    [InlineData(WindowState.Maximized, WindowDecorations.BorderOnly, WindowDecorationsElementRole.TitleBar, (int)HitTestValues.HTCAPTION)]
    [InlineData(WindowState.Maximized, WindowDecorations.None, WindowDecorationsElementRole.TitleBar, (int)HitTestValues.HTCAPTION)]
    [InlineData(WindowState.Maximized, WindowDecorations.BorderOnly, WindowDecorationsElementRole.ResizeSE, (int)HitTestValues.HTBOTTOMRIGHT)]
    public async Task Move_And_Resize_Roles_Are_Client_Area_In_FullScreen(
        WindowState windowedState,
        WindowDecorations decorations,
        WindowDecorationsElementRole role,
        int windowedHitTest)
    {
        var element = new Border
        {
            Width = 100,
            Height = 40,
            Margin = new Thickness(0, 60, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
            Background = Brushes.DodgerBlue
        };
        WindowDecorationProperties.SetElementRole(element, role);

        _window = new Window
        {
            WindowState = windowedState,
            WindowDecorations = decorations,
            ExtendClientAreaToDecorationsHint = true,
            Width = 300,
            Height = 300,
            Content = new Panel { Children = { element } }
        };

        _window.Show();
        await Window.WhenLoadedAsync();

        Assert.Equal(windowedState, Window.WindowState);
        Assert.Equal((HitTestValues)windowedHitTest, await NonClientHitTestAsync(element));

        // A fullscreen window has no caption or frame: the element must not start a system move or size loop.
        Window.WindowState = WindowState.FullScreen;
        Assert.Equal(HitTestValues.HTCLIENT, await NonClientHitTestAsync(element));

        Window.WindowState = windowedState;
        Assert.Equal((HitTestValues)windowedHitTest, await NonClientHitTestAsync(element));
    }

    private async Task<HitTestValues> NonClientHitTestAsync(Visual element)
    {
        // Chrome hit-testing uses the compositor's hit-test data, which is only updated once the render thread has
        // rendered the window, possibly some time after the UI thread's frame: wait until it finds the element.
        var center = element.TranslatePoint(new Point(element.Bounds.Width / 2, element.Bounds.Height / 2), Window);
        Assert.NotNull(center);

        await WhenFrameRenderedAsync();

        var stopwatch = Stopwatch.StartNew();
        while (Window.GetVisualAt(center.Value) != element && stopwatch.Elapsed < TimeSpan.FromSeconds(5))
            await Task.Delay(10, TestContext.Current.CancellationToken);

        Assert.Same(element, Window.GetVisualAt(center.Value));
        return NonClientHitTest(element);
    }

    private Task WhenFrameRenderedAsync()
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Window.RequestAnimationFrame(_ => tcs.TrySetResult());
        return tcs.Task;
    }

    private HitTestValues NonClientHitTest(Visual element)
    {
        var platformHandle = Window.TryGetPlatformHandle();
        Assert.NotNull(platformHandle);

        var point = element.PointToScreen(new Point(element.Bounds.Width / 2, element.Bounds.Height / 2));
        var lParam = (IntPtr)((point.Y << 16) | (point.X & 0xFFFF));
        return (HitTestValues)(int)SendMessage(platformHandle.Handle, (int)WindowsMessage.WM_NCHITTEST, IntPtr.Zero, lParam);
    }

    public void Dispose()
        => _window?.Close();
}
