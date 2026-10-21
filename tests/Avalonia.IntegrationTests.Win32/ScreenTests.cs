using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Rendering;
using Avalonia.Threading;
using Avalonia.Win32;
using Xunit;
using Win32Methods = Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class ScreenTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Display_Changes_Refresh_Screens_With_Or_Without_A_Window(bool createWindow)
    {
        var platform = Win32Platform.Instance;
        var screens = platform.Screen;
        var window = createWindow ? platform.CreateWindow() : null;
        var cachedScreens = screens.AllScreens;
        var changes = 0;
        void OnChanged() => changes++;
        screens.Changed += OnChanged;

        try
        {
            // Deliver the broadcast to this application's HWNDs without changing the desktop.
            SendDisplayChange(platform.Handle);
            SendDisplayChange(platform.Handle);
            if (window is not null)
                SendDisplayChange(window.Handle!.Handle);

            Assert.NotSame(cachedScreens, screens.AllScreens);
            Assert.Equal(screens.ScreenCount, screens.AllScreens.Count);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background,
                TestContext.Current.CancellationToken);
            Assert.Equal(1, changes);
        }
        finally
        {
            screens.Changed -= OnChanged;
            window?.Dispose();
        }
    }

    [Fact]
    public async Task Display_Changes_Update_Render_Timer_Without_A_Window()
    {
        using var scope = AvaloniaLocator.EnterScope();
        var timer = new SleepLoopRenderTimer(1);
        AvaloniaLocator.CurrentMutable.Bind<IRenderLoop>().ToConstant(RenderLoop.FromTimer(timer));
        var platform = Win32Platform.Instance;
        var expectedFps = Math.Max(60, platform.Screen.AllScreens.Cast<WinScreen>().Max(s => s.Frequency));

        SendDisplayChange(platform.Handle);

        Assert.Equal(expectedFps, timer.DesiredFps);
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.Background,
            TestContext.Current.CancellationToken);
    }

    private static void SendDisplayChange(IntPtr hwnd)
        => Win32Methods.SendMessage(hwnd, (uint)Win32Methods.WindowsMessage.WM_DISPLAYCHANGE,
            IntPtr.Zero, IntPtr.Zero);
}
