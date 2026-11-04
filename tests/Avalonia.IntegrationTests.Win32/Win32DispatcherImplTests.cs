using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using Avalonia.Threading;
using Avalonia.Win32;
using Xunit;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.IntegrationTests.Win32;

public class Win32DispatcherImplTests
{
    [Fact]
    public void RunLoop_Returns_When_Its_Token_Is_Cancelled_From_Another_Thread_While_Idle()
    {
        using var cts = new CancellationTokenSource();
        using var windowCreated = new ManualResetEventSlim();
        using var loopReturned = new ManualResetEventSlim();
        var hwnd = IntPtr.Zero;
        Exception? error = null;

        // The loop gets a thread and a message window of its own, so no other message can wake it.
        var thread = new Thread(() =>
        {
            WndProc wndProc = DefWindowProc;
            var className = "Win32DispatcherImplTests " + Guid.NewGuid();
            var wndClass = new WNDCLASSEX
            {
                cbSize = Marshal.SizeOf<WNDCLASSEX>(),
                lpfnWndProc = wndProc,
                hInstance = GetModuleHandle(null),
                lpszClassName = className,
            };

            try
            {
                var atom = RegisterClassEx(ref wndClass);
                if (atom == 0)
                    throw new Win32Exception();
                hwnd = CreateWindowEx(0, atom, null, 0, 0, 0, 0, 0, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (hwnd == IntPtr.Zero)
                    throw new Win32Exception();
                windowCreated.Set();

                var impl = new Win32DispatcherImpl(hwnd);
                cts.CancelAfter(100); // cancels from a thread pool timer
                impl.RunLoop(cts.Token);
                loopReturned.Set();
            }
            catch (Exception e)
            {
                error = e;
            }
            finally
            {
                if (hwnd != IntPtr.Zero)
                    DestroyWindow(hwnd);
                UnregisterClass(className, GetModuleHandle(null));
                GC.KeepAlive(wndProc);
            }
        });
        thread.Start();

        var returned = loopReturned.Wait(TimeSpan.FromSeconds(5), TestContext.Current.CancellationToken);
        if (!returned && windowCreated.IsSet)
        {
            // End the loop so the thread can exit
            PostMessage(hwnd, (uint)WindowsMessage.WM_QUIT, IntPtr.Zero, IntPtr.Zero);
        }
        Assert.True(thread.Join(TimeSpan.FromSeconds(5)), "The loop thread didn't exit");

        // Creating the impl made the test's thread the Win32 loop thread; give that back to the UI thread.
        Dispatcher.UIThread.Invoke(() => new Win32DispatcherImpl(Win32Platform.Instance.Handle));

        Assert.Null(error);
        Assert.True(returned, "RunLoop didn't return after its token was cancelled");
    }
}
