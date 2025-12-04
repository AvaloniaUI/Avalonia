using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Controls.Chrome;
using Avalonia.Interactivity;
using Avalonia.Media;
using Avalonia.Platform;
using Avalonia.VisualTree;
using Xunit;

namespace Avalonia.IntegrationTests.Win32;

public abstract class ExtendClientAreaTests : IDisposable
{
    private const double ClientWidth = 200;
    private const double ClientHeight = 200;

    private Window? _window;

    private Window Window
    {
        get
        {
            Assert.NotNull(_window);
            return _window;
        }
    }

    private IntPtr Handle
    {
        get
        {
            var platformHandle = Window.TryGetPlatformHandle();
            Assert.NotNull(platformHandle);
            return platformHandle.Handle;
        }
    }

    protected abstract SystemDecorations Decorations { get; }

    public static MatrixTheoryData<bool, WindowState> States
        => new([true, false], Enum.GetValues<WindowState>());

    private async Task InitWindowAsync(WindowState state, bool canResize)
    {
        Assert.Null(_window);

        _window = new Window
        {
            CanResize = canResize,
            WindowState = state,
            SystemDecorations = Decorations,
            ExtendClientAreaToDecorationsHint = true,
            ExtendClientAreaChromeHints = ExtendClientAreaChromeHints.PreferSystemChrome,
            Width = ClientWidth,
            Height = ClientHeight,
            WindowStartupLocation = WindowStartupLocation.Manual,
            Position = new PixelPoint(50, 50),
            Content = new Border
            {
                Background = Brushes.DodgerBlue,
                BorderBrush = Brushes.Yellow,
                BorderThickness = new Thickness(1)
            }
        };

        _window.Show();

        await WhenWindowLoadedAsync();
    }

    private Task WhenWindowLoadedAsync()
    {
        var window = Window;
        if (window.IsLoaded)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource();
        window.Loaded += OnLoaded;
        return tcs.Task;

        void OnLoaded(object? sender, RoutedEventArgs e)
        {
            window.Loaded -= OnLoaded;
            tcs.TrySetResult();
        }
    }

    private Screen GetScreen()
    {
        var screen = Window.Screens.ScreenFromWindow(Window);
        Assert.NotNull(screen);
        return screen;
    }

    private PixelSize GetClientSize()
    {
        Assert.True(UnmanagedMethods.GetClientRect(Handle, out var rect));
        return rect.ToPixelRect().Size;
    }

    private PixelRect GetWindowBounds()
    {
        Assert.True(UnmanagedMethods.GetWindowRect(Handle, out var rect));
        return rect.ToPixelRect();
    }

    [Theory]
    [MemberData(nameof(States))]
    public async Task Normal_State_Respects_Client_Size(bool canResize, WindowState initialState)
    {
        await InitWindowAsync(initialState, canResize);

        if (initialState != WindowState.Normal)
            Window.WindowState = WindowState.Normal;

        // The client size should have been kept
        var expected = PixelSize.FromSize(new Size(ClientWidth, ClientHeight), Window.RenderScaling);
        var clientSize = GetClientSize();
        Assert.Equal(expected, clientSize);

        VerifyNormalState(canResize);
    }

    protected abstract void VerifyNormalState(bool canResize);

    [Theory]
    [MemberData(nameof(States))]
    public async Task Maximized_State_Fills_Screen_Working_Area(bool canResize, WindowState initialState)
    {
        await InitWindowAsync(initialState, canResize);

        if (initialState != WindowState.Maximized)
            Window.WindowState = WindowState.Maximized;

        // The client size should match the screen working area
        var clientSize = GetClientSize();
        var screenWorkingArea = GetScreen().WorkingArea;
        Assert.Equal(screenWorkingArea.Size, clientSize);

        VerifyMaximizedState();
    }

    protected abstract void VerifyMaximizedState();

    [Theory]
    [MemberData(nameof(States))]
    public async Task FullScreen_State_Fills_Screen(bool canResize, WindowState initialState)
    {
        await InitWindowAsync(initialState, canResize);

        if (initialState != WindowState.FullScreen)
            Window.WindowState = WindowState.FullScreen;

        // The client size should match the screen bounds
        var clientSize = GetClientSize();
        var screenBounds = GetScreen().Bounds;
        Assert.Equal(screenBounds.Width, clientSize.Width);
        Assert.Equal(screenBounds.Height, clientSize.Height);

        // The window size should also match the screen bounds
        var windowBounds = GetWindowBounds();
        Assert.Equal(screenBounds, windowBounds);

        // And no visible title bar
        AssertNoTitleBar();
    }

    protected void AssertHasBorder()
    {
        var clientSize = GetClientSize();
        var windowBounds = GetWindowBounds();
        Assert.NotEqual(clientSize.Width, windowBounds.Width);
        Assert.NotEqual(clientSize.Height, windowBounds.Height);
    }

    protected void AssertNoBorder()
    {
        var clientSize = GetClientSize();
        var windowBounds = GetWindowBounds();
        Assert.Equal(clientSize.Width, windowBounds.Width);
        Assert.Equal(clientSize.Height, windowBounds.Height);
    }

    protected (double TitleBarHeight, double ButtonsHeight) GetTitleBarInfo()
    {
        var titleBar = Window.GetVisualDescendants().OfType<TitleBar>().FirstOrDefault();
        Assert.NotNull(titleBar);

        var buttons = titleBar.GetVisualDescendants().OfType<CaptionButtons>().FirstOrDefault();
        Assert.NotNull(buttons);

        return (titleBar.Height, buttons.Height);
    }

    private void AssertNoTitleBar()
    {
        var (titleBarHeight, buttonsHeight) = GetTitleBarInfo();
        Assert.Equal(0, titleBarHeight);
        Assert.Equal(0, buttonsHeight);
    }

    public void Dispose()
        => _window?.Close();

    public sealed class DecorationsFull : ExtendClientAreaTests
    {
        protected override SystemDecorations Decorations
            => SystemDecorations.Full;

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

    public sealed class DecorationsBorderOnly : ExtendClientAreaTests
    {
        protected override SystemDecorations Decorations
            => SystemDecorations.BorderOnly;

        protected override void VerifyNormalState(bool canResize)
        {
            AssertHasBorder();

            if (canResize)
                AssertSmallTitleBarWithoutButtons();
            else
                AssertNoTitleBar();
        }

        protected override void VerifyMaximizedState()
        {
            AssertNoBorder();
            AssertNoTitleBar();
        }

        private void AssertSmallTitleBarWithoutButtons()
        {
            var (titleBarHeight, buttonsHeight) = GetTitleBarInfo();
            Assert.True(titleBarHeight < 10);
            Assert.NotEqual(0, titleBarHeight);
            Assert.Equal(0, buttonsHeight);
        }
    }

    public sealed class DecorationsNone : ExtendClientAreaTests
    {
        protected override SystemDecorations Decorations
            => SystemDecorations.None;

        protected override void VerifyNormalState(bool canResize)
        {
            AssertNoBorder();
            AssertNoTitleBar();
        }

        protected override void VerifyMaximizedState()
        {
            AssertNoBorder();
            AssertNoTitleBar();
        }
    }
}
