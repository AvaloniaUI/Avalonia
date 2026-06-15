using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Media;
using Xunit;
using static Avalonia.IntegrationTests.Win32.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public abstract class StandardWindowTests : IDisposable
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

    protected abstract bool HasCaption { get; }

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
            ExtendClientAreaToDecorationsHint = false,
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
    public async Task Maximized_State_Fills_Screen_Working_Area(int screenIndex, WindowState initialState, bool canResize)
    {
        await InitWindowAsync(screenIndex, initialState, canResize);

        if (initialState != WindowState.Maximized)
            Window.WindowState = WindowState.Maximized;

        // The client size should match the screen working area
        var clientSize = Window.GetWin32ClientSize();
        var screenWorkingArea = Window.GetScreenAtIndex(screenIndex).WorkingArea;

        if (HasCaption)
        {
            Assert.Equal(screenWorkingArea.Size.Width, clientSize.Width);
            Assert.True(clientSize.Height < screenWorkingArea.Size.Height);
        }
        else
            Assert.Equal(screenWorkingArea.Size, clientSize);
    }

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
    }

    public void Dispose()
        => _window?.Close();

    public sealed class DecorationsFull : StandardWindowTests
    {
        protected override WindowDecorations Decorations
            => WindowDecorations.Full;

        protected override bool HasCaption
            => true;
    }

    public sealed class DecorationsBorderOnly : StandardWindowTests
    {
        protected override WindowDecorations Decorations
            => WindowDecorations.BorderOnly;

        protected override bool HasCaption
            => false;
    }

    public sealed class DecorationsNone : StandardWindowTests
    {
        protected override WindowDecorations Decorations
            => WindowDecorations.None;

        protected override bool HasCaption
            => false;
    }
}
