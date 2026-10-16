using System;
using System.Threading.Tasks;
using Avalonia.Controls;
using Xunit;
using static Avalonia.IntegrationTests.Win32.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class WindowStateTests : IDisposable
{
    private Window? _window;

    [Fact]
    public async Task Resized_Reports_The_New_WindowState()
    {
        _window = new Window
        {
            Width = 200,
            Height = 200,
            WindowStartupLocation = WindowStartupLocation.Manual
        };

        _window.Show();
        await _window.WhenLoadedAsync();

        WindowState? stateDuringResize = null;
        _window.Resized += (_, _) => stateDuringResize ??= _window.WindowState;

        var handle = _window.TryGetPlatformHandle();
        Assert.NotNull(handle);

        // Maximizing through the OS is the path a user takes; setting WindowState from code updates the property first and hides the bug.
        ShowWindow(handle.Handle, SW_MAXIMIZE);

        Assert.Equal(WindowState.Maximized, stateDuringResize);
    }

    public void Dispose()
        => _window?.Close();
}
