using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform;
using Xunit;

namespace Avalonia.IntegrationTests.Win32;

internal static class WindowExtensions
{
    public static PixelRect ToPixelRect(this UnmanagedMethods.RECT rect)
        => new(new PixelPoint(rect.left, rect.top), new PixelPoint(rect.right, rect.bottom));

    public static Task WhenLoadedAsync(this Window window)
    {
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

    public static Screen GetScreen(this Window window)
    {
        var screen = window.Screens.ScreenFromWindow(window);
        Assert.NotNull(screen);
        return screen;
    }

    public static PixelSize GetWin32ClientSize(this Window window)
    {
        var platformHandle = window.TryGetPlatformHandle();
        Assert.NotNull(platformHandle);

        Assert.True(UnmanagedMethods.GetClientRect(platformHandle.Handle, out var rect));
        return rect.ToPixelRect().Size;
    }

    public static PixelRect GetWin32WindowBounds(this Window window)
    {
        var platformHandle = window.TryGetPlatformHandle();
        Assert.NotNull(platformHandle);

        Assert.True(UnmanagedMethods.GetWindowRect(platformHandle.Handle, out var rect));
        return rect.ToPixelRect();
    }

}
