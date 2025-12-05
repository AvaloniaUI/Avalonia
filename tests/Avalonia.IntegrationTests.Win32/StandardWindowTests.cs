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

public abstract class StandardWindowTests : IDisposable
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

    protected abstract SystemDecorations Decorations { get; }

    protected abstract bool HasCaption { get; }

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
            ExtendClientAreaToDecorationsHint = false,
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

        await Window.WhenLoadedAsync();
    }

    [Theory]
    [MemberData(nameof(States))]
    public async Task Maximized_State_Fills_Screen_Working_Area(bool canResize, WindowState initialState)
    {
        await InitWindowAsync(initialState, canResize);

        if (initialState != WindowState.Maximized)
            Window.WindowState = WindowState.Maximized;

        // The client size should match the screen working area
        var clientSize = Window.GetWin32ClientSize();
        var screenWorkingArea = Window.GetScreen().WorkingArea;

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
    public async Task FullScreen_State_Fills_Screen(bool canResize, WindowState initialState)
    {
        await InitWindowAsync(initialState, canResize);

        if (initialState != WindowState.FullScreen)
            Window.WindowState = WindowState.FullScreen;

        // The client size should match the screen bounds
        var clientSize = Window.GetWin32ClientSize();
        var screenBounds = Window.GetScreen().Bounds;
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
        protected override SystemDecorations Decorations
            => SystemDecorations.Full;

        protected override bool HasCaption
            => true;
    }

    public sealed class DecorationsBorderOnly : StandardWindowTests
    {
        protected override SystemDecorations Decorations
            => SystemDecorations.BorderOnly;

        protected override bool HasCaption
            => false;
    }

    public sealed class DecorationsNone : StandardWindowTests
    {
        protected override SystemDecorations Decorations
            => SystemDecorations.None;

        protected override bool HasCaption
            => false;
    }
}
