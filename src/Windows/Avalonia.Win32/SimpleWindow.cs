using System;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Runtime.InteropServices;
using Avalonia.Win32.Interop;

namespace Avalonia.Win32;

internal class SimpleWindow : IDisposable
{
    private readonly UnmanagedMethods.WndProc? _wndProc;
    private static UnmanagedMethods.WndProc s_wndProcDelegate;
    public IntPtr Handle { get; private set; }
    private static string s_className;
    private static uint s_classAtom;
    private static ConcurrentDictionary<IntPtr, SimpleWindow> s_Instances = new();

    static SimpleWindow()
    {
        s_wndProcDelegate = WndProc;
        var wndClassEx = new UnmanagedMethods.WNDCLASSEX
        {
            cbSize = Marshal.SizeOf<UnmanagedMethods.WNDCLASSEX>(),
            hInstance = UnmanagedMethods.GetModuleHandle(null),
            lpfnWndProc = s_wndProcDelegate,
            lpszClassName = s_className = "AvaloniaSimpleWindow-" + Guid.NewGuid(),
        };

        s_classAtom = UnmanagedMethods.RegisterClassEx(ref wndClassEx);
    }

    public SimpleWindow(UnmanagedMethods.WndProc? wndProc)
    {
        _wndProc = wndProc;
        var handle = GCHandle.Alloc(this);
        try
        {
            var hwnd = UnmanagedMethods.CreateWindowEx(
                0,
                s_classAtom,
                null,
                (int)UnmanagedMethods.WindowStyles.WS_OVERLAPPEDWINDOW,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                UnmanagedMethods.CW_USEDEFAULT,
                IntPtr.Zero,
                IntPtr.Zero,
                IntPtr.Zero,
                GCHandle.ToIntPtr(handle));
            if (hwnd == IntPtr.Zero)
            {
                throw new Win32Exception();
            }

            Handle = hwnd;
        }
        finally
        {
            handle.Free();
        }
    }

    private static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
    {
        SimpleWindow? window;
        if (msg == (uint)UnmanagedMethods.WindowsMessage.WM_CREATE)
        {
            var handle = Marshal.ReadIntPtr(lParam);
            window = (SimpleWindow?)GCHandle.FromIntPtr(handle).Target;
            if (window == null)
                return IntPtr.Zero;
            s_Instances.TryAdd(hWnd, window);
        }
        else
        {
            s_Instances.TryGetValue(hWnd, out window);
        }

        if (msg == (uint)UnmanagedMethods.WindowsMessage.WM_DESTROY)
            s_Instances.TryRemove(hWnd, out _);
            
        return window?._wndProc?.Invoke(hWnd, msg, wParam, lParam)
               ?? UnmanagedMethods.DefWindowProc(hWnd, msg, wParam, lParam);
    }

    public void Dispose()
    {
        UnmanagedMethods.DestroyWindow(Handle);
        Handle = IntPtr.Zero;
    }
}
