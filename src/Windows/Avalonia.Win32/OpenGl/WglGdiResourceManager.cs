using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.OpenGl;

/// <summary>
/// - ReleaseDC can only happen from the same thread that has called GetDC
/// - When thread exits all of its windows and HDCs are getting destroyed
/// - We need to create OpenGL context (require a window and an HDC) and render targets (require an HDC) from thread pool threads
///
/// So this class hosts a dedicated thread for managing offscreen windows and HDCs for OpenGL
/// </summary>

internal class WglGdiResourceManager
{
    private class GetDCOp
    {
        public readonly IntPtr Window;
        public readonly TaskCompletionSource<IntPtr> Result;

        public GetDCOp(IntPtr window, TaskCompletionSource<IntPtr> result)
        {
            Window = window;
            Result = result;
        }
    }

    private class ReleaseDCOp
    {
        public readonly IntPtr Window;
        public readonly IntPtr DC;
        public readonly TaskCompletionSource<object?> Result;

        public ReleaseDCOp(IntPtr window, IntPtr dc, TaskCompletionSource<object?> result)
        {
            Window = window;
            DC = dc;
            Result = result;
        }
    }

    private class CreateWindowOp
    {
        public readonly TaskCompletionSource<IntPtr> Result;

        public CreateWindowOp(TaskCompletionSource<IntPtr> result)
        {
            Result = result;
        }
    }

    private class DestroyWindowOp
    {
        public readonly IntPtr Window;
        public readonly TaskCompletionSource<object?> Result;

        public DestroyWindowOp(IntPtr window, TaskCompletionSource<object?> result)
        {
            Window = window;
            Result = result;
        }
    }

    private static readonly Queue<object> s_queue = new();
    private static readonly AutoResetEvent s_event = new(false);
    private static readonly ushort s_windowClass;
    private static readonly UnmanagedMethods.WndProc s_wndProcDelegate = WndProc;

    private static void Worker()
    {
        while (true)
        {
            s_event.WaitOne();
            lock (s_queue)
            {
                if(s_queue.Count == 0)
                    continue;
                var job = s_queue.Dequeue();
                if (job is GetDCOp getDc)
                    getDc.Result.TrySetResult(UnmanagedMethods.GetDC(getDc.Window));
                else if (job is ReleaseDCOp releaseDc)
                {
                    UnmanagedMethods.ReleaseDC(releaseDc.Window, releaseDc.DC);
                    releaseDc.Result.SetResult(null);
                }
                else if (job is CreateWindowOp createWindow)
                    createWindow.Result.TrySetResult(UnmanagedMethods.CreateWindowEx(
                        0,
                        s_windowClass,
                        null,
                        (int)UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW,
                        0,
                        0,
                        640,
                        480,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero,
                        IntPtr.Zero));
                else if (job is DestroyWindowOp destroyWindow)
                {
                    UnmanagedMethods.DestroyWindow(destroyWindow.Window);
                    destroyWindow.Result.TrySetResult(null);
                }
            }
        }
    }

    static WglGdiResourceManager()
    {
        var wndClassEx = new UnmanagedMethods.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
            hInstance = UnmanagedMethods.GetModuleHandle(null),
            lpfnWndProc = s_wndProcDelegate,
            lpszClassName = "AvaloniaGlWindow-" + Guid.NewGuid(),
            style = (int)UnmanagedMethods.ClassStyles.CS_OWNDC
        };
            
        s_windowClass = UnmanagedMethods.RegisterClassEx(ref wndClassEx);
        var th = new Thread(Worker) { IsBackground = true, Name = "Win32 OpenGL HDC manager" };
        // This makes CLR to automatically pump the event queue from WaitOne
        th.SetApartmentState(ApartmentState.STA);
        th.Start();
    }


    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }
    
    public static IntPtr CreateOffscreenWindow()
    {
        var tcs = new TaskCompletionSource<IntPtr>();
        lock (s_queue)
            s_queue.Enqueue(new CreateWindowOp(tcs));
        s_event.Set();
        return tcs.Task.Result;
    }
    
    public static IntPtr GetDC(IntPtr hWnd)
    {
        var tcs = new TaskCompletionSource<IntPtr>();
        lock (s_queue)
            s_queue.Enqueue(new GetDCOp(hWnd, tcs));
        s_event.Set();
        return tcs.Task.Result;
    }

    public static void ReleaseDC(IntPtr hWnd, IntPtr hDC)
    {
        var tcs = new TaskCompletionSource<object?>();
        lock (s_queue)
            s_queue.Enqueue(new ReleaseDCOp(hWnd, hDC, tcs));
        s_event.Set();
        tcs.Task.Wait();
    }
    
    public static void DestroyWindow(IntPtr hWnd)
    {
        var tcs = new TaskCompletionSource<object?>();
        lock (s_queue)
            s_queue.Enqueue(new DestroyWindowOp(hWnd, tcs));
        s_event.Set();
        tcs.Task.Wait();
    }
}
