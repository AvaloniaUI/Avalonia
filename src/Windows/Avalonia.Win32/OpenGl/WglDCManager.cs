using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32.OpenGl;

/// <summary>
/// 1) ReleaseDC can only happen from the same thread that has called GetDC
/// 2) When thread exits all of its HDCs are getting destroyed
/// 3) We need to create OpenGL render targets from thread pool threads
///
/// So this class hosts a dedicated thread for managing HDCs for OpenGL
/// </summary>

internal class WglDCManager
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

    private static readonly Queue<object> s_Queue = new();
    private static readonly AutoResetEvent s_Event = new(false);

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
            }
        }
    }

    static WglDCManager()
    {
        new Thread(Worker) { IsBackground = true }.Start();
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
}
