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
    class GetDCOp
    {
        public IntPtr Window;
        public TaskCompletionSource<IntPtr> Result;
    }

    class ReleaseDCOp
    {
        public IntPtr Window;
        public IntPtr DC;
        public TaskCompletionSource<object> Result;
    }
    
    class CreateWindowOp
    {
        public TaskCompletionSource<IntPtr> Result;
    }

    class DestroyWindowOp
    {
        public IntPtr Window;
        public TaskCompletionSource<object> Result;
    }

    private static readonly Queue<object> s_Queue = new();
    private static readonly AutoResetEvent s_Event = new(false);
    private static readonly ushort s_WindowClass;
    private static readonly UnmanagedMethods.WndProc s_wndProcDelegate = WndProc;

    static void Worker()
    {
        while (true)
        {
            s_Event.WaitOne();
            lock (s_Queue)
            {
                if(s_Queue.Count == 0)
                    continue;
                var job = s_Queue.Dequeue();
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
                        s_WindowClass,
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
            
        s_WindowClass = UnmanagedMethods.RegisterClassEx(ref wndClassEx);
        var th = new Thread(Worker) { IsBackground = true, Name = "Win32 OpenGL HDC manager" };
        // This makes CLR to automatically pump the event queue from WaitOne
        th.SetApartmentState(ApartmentState.STA);
        th.Start();
    }
    
        
    
    static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        return UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }
    
    public static IntPtr CreateOffscreenWindow()
    {
        var tcs = new TaskCompletionSource<IntPtr>();
        lock(s_Queue)
            s_Queue.Enqueue(new CreateWindowOp()
            {
                Result = tcs
            });
        s_Event.Set();
        return tcs.Task.Result;
    }
    
    public static IntPtr GetDC(IntPtr hWnd)
    {
        var tcs = new TaskCompletionSource<IntPtr>();
        lock(s_Queue)
            s_Queue.Enqueue(new GetDCOp
            {
                Window = hWnd,
                Result = tcs
            });
        s_Event.Set();
        return tcs.Task.Result;
    }

    public static void ReleaseDC(IntPtr hWnd, IntPtr hDC)
    {
        var tcs = new TaskCompletionSource<object>();
        lock(s_Queue)
            s_Queue.Enqueue(new ReleaseDCOp()
            {
                Window = hWnd,
                DC = hDC,
                Result = tcs
            });
        s_Event.Set();
        tcs.Task.Wait();
    }
    
    public static void DestroyWindow(IntPtr hWnd)
    {
        var tcs = new TaskCompletionSource<object>();
        lock(s_Queue)
            s_Queue.Enqueue(new DestroyWindowOp()
            {
                Window = hWnd,
                Result = tcs
            });
        s_Event.Set();
        tcs.Task.Wait();
        
    }
}
