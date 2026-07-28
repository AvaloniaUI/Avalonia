using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.VisualTree;
using Xunit;
using static Avalonia.IntegrationTests.Win32.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public abstract class ExtendClientAreaWindowTests : IDisposable
{
    private const int ClientWidth = 200;
    private const int ClientHeight = 200;

    private Window? _window;

    private Window Window
    {
        get
        {
            Assert.NotNull(_window);
            return _window;
        }
    }

    protected abstract WindowDecorations Decorations { get; }

    public static MatrixTheoryData<int, WindowState, bool> States
        => new(
            Enumerable.Range(0, GetSystemMetrics(SM_CMONITORS)),
            Enum.GetValues<WindowState>(),
            [true, false]);

    private async Task InitWindowAsync(int screenIndex, WindowState state, bool canResize)
    {
        Assert.Null(_window);

        _window = new Window
        {
            CanResize = canResize,
            WindowState = state,
            WindowDecorations = Decorations,
            ExtendClientAreaToDecorationsHint = true,
            Width = ClientWidth,
            Height = ClientHeight,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Content = new Border
            {
                Background = Brushes.DodgerBlue,
                BorderBrush = Brushes.Yellow,
                BorderThickness = new Thickness(1)
            }
        };

        var screenCenter = _window.Screens.All[screenIndex].Bounds.Center;
        _window.Position = new PixelPoint(screenCenter.X - ClientWidth / 2, screenCenter.Y - ClientHeight / 2);

        _window.Show();

        await Window.WhenLoadedAsync();
    }

    [Theory]
    [MemberData(nameof(States))]
    public async Task Normal_State_Respects_Client_Size(int screenIndex, WindowState initialState, bool canResize)
    {
        await InitWindowAsync(screenIndex, initialState, canResize);

        if (initialState != WindowState.Normal)
            Window.WindowState = WindowState.Normal;

        // The client size should have been kept
        var expected = PixelSize.FromSize(new Size(ClientWidth, ClientHeight), Window.RenderScaling);
        var clientSize = Window.GetWin32ClientSize();
        Assert.Equal(expected, clientSize);

        VerifyNormalState(canResize);
    }

    protected abstract void VerifyNormalState(bool canResize);

    [Theory]
    [MemberData(nameof(States))]
    public async Task Maximized_State_Fills_Screen_Working_Area(int screenIndex, WindowState initialState, bool canResize)
    {
        await InitWindowAsync(screenIndex, initialState, canResize);

        if (initialState != WindowState.Maximized)
            Window.WindowState = WindowState.Maximized;

        // The client size should match the screen working area
        var clientSize = Window.GetWin32ClientSize();
        var screenWorkingArea = Window.GetScreenAtIndex(screenIndex).WorkingArea;
        Assert.Equal(screenWorkingArea.Size, clientSize);

        VerifyMaximizedState();
    }

    protected abstract void VerifyMaximizedState();

    [Theory]
    [MemberData(nameof(States))]
    public async Task FullScreen_State_Fills_Screen(int screenIndex, WindowState initialState, bool canResize)
    {
        await InitWindowAsync(screenIndex, initialState, canResize);

        if (initialState != WindowState.FullScreen)
            Window.WindowState = WindowState.FullScreen;

        // The client size should match the screen bounds
        var clientSize = Window.GetWin32ClientSize();
        var screenBounds = Window.GetScreenAtIndex(screenIndex).Bounds;
        Assert.Equal(screenBounds.Width, clientSize.Width);
        Assert.Equal(screenBounds.Height, clientSize.Height);

        // The window size should also match the screen bounds
        var windowBounds = Window.GetWin32WindowBounds();
        Assert.Equal(screenBounds, windowBounds);

        // And no visible title bar
        AssertNoTitleBar();
    }

    protected void AssertHasBorder()
    {
        var clientSize = Window.GetWin32ClientSize();
        var windowBounds = Window.GetWin32WindowBounds();
        Assert.NotEqual(clientSize.Width, windowBounds.Width);
        Assert.NotEqual(clientSize.Height, windowBounds.Height);
    }

    protected void AssertNoBorder()
    {
        var clientSize = Window.GetWin32ClientSize();
        var windowBounds = Window.GetWin32WindowBounds();
        Assert.Equal(clientSize.Width, windowBounds.Width);
        Assert.Equal(clientSize.Height, windowBounds.Height);
    }

    protected (double TitleBarHeight, double ButtonsHeight) GetTitleBarInfo()
    {
        var host = Window.GetVisualParent()!;
        host.GetLayoutManager()!.ExecuteLayoutPass();

        var titlebar = host.GetVisualDescendants().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "AvaloniaTitleBar");
        var closeButton = host.GetVisualDescendants().FirstOrDefault(c => AutomationProperties.GetAutomationId(c) == "Close");
        
        return (
            titlebar?.IsEffectivelyVisible == true ? titlebar.Bounds.Height : 0,
            closeButton?.IsEffectivelyVisible == true ? closeButton.Bounds.Height : 0);
    }

    private void AssertNoTitleBar()
    {
        var (titleBarHeight, buttonsHeight) = GetTitleBarInfo();
        Assert.Equal(0, titleBarHeight);
        Assert.Equal(0, buttonsHeight);
    }

    public void Dispose()
        => _window?.Close();

    public sealed class DecorationsFull : ExtendClientAreaWindowTests
    {
        protected override WindowDecorations Decorations
            => WindowDecorations.Full;

        protected override void VerifyNormalState(bool canResize)
        {
            AssertHasBorder();
            AssertLargeTitleBarWithButtons();
        }

        protected override void VerifyMaximizedState()
            => AssertLargeTitleBarWithButtons();

        private void AssertLargeTitleBarWithButtons()
        {
            var (titleBarHeight, buttonsHeight) = GetTitleBarInfo();
            Assert.True(titleBarHeight > 20);
            Assert.True(buttonsHeight > 20);
        }
    }

    public sealed class DecorationsBorderOnly : ExtendClientAreaWindowTests
    {
        protected override WindowDecorations Decorations
            => WindowDecorations.BorderOnly;

        protected override void VerifyNormalState(bool canResize)
        {
            AssertHasBorder();
            AssertNoTitleBar();
        }

        protected override void VerifyMaximizedState()
            => AssertNoTitleBar();
    }

    public sealed class DecorationsNone : ExtendClientAreaWindowTests
    {
        protected override WindowDecorations Decorations
            => WindowDecorations.None;

        protected override void VerifyNormalState(bool canResize)
        {
            AssertNoBorder();
            AssertNoTitleBar();
        }

        protected override void VerifyMaximizedState()
            => AssertNoTitleBar();
    }
}
