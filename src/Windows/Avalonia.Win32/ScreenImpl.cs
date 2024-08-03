using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using Windows.Win32;
using static Avalonia.Win32.Interop.UnmanagedMethods;
using winmdroot = global::Windows.Win32;

namespace Avalonia.Win32;

internal unsafe class ScreenImpl : ScreensBase<nint, WinScreen>
{
    protected override int GetScreenCount() => GetSystemMetrics(SystemMetric.SM_CMONITORS);

    protected override IReadOnlyList<nint> GetAllScreenKeys()
    {
        var screens = new List<nint>();
        var gcHandle = GCHandle.Alloc(screens);
        try
        {
            PInvoke.EnumDisplayMonitors(default, default(winmdroot.Foundation.RECT*), EnumDisplayMonitorsCallback, (IntPtr)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }

        return screens;

        static winmdroot.Foundation.BOOL EnumDisplayMonitorsCallback(
            winmdroot.Graphics.Gdi.HMONITOR monitor,
            winmdroot.Graphics.Gdi.HDC hdcMonitor,
            winmdroot.Foundation.RECT* lprcMonitor,
            winmdroot.Foundation.LPARAM dwData)
        {
            if (GCHandle.FromIntPtr(dwData).Target is List<nint> screens)
            {
                screens.Add(monitor);
                return true;
            }
            return false;
        }
    }

    protected override WinScreen CreateScreenFromKey(nint key) => new(key);
    protected override void ScreenChanged(WinScreen screen) => screen.Refresh();

    protected override Screen? ScreenFromTopLevelCore(ITopLevelImpl topLevel)
    {
        if (topLevel.Handle?.Handle is { } handle)
        {
            return ScreenFromHwnd(handle);
        }

        return null;
    }

    protected override Screen? ScreenFromPointCore(PixelPoint point)
    {
        var monitor = MonitorFromPoint(new POINT
        {
            X = point.X,
            Y = point.Y
        }, UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONULL);

        return ScreenFromHMonitor(monitor);
    }

    protected override Screen? ScreenFromRectCore(PixelRect rect)
    {
        var monitor = MonitorFromRect(new RECT
        {
            left = rect.TopLeft.X,
            top = rect.TopLeft.Y,
            right = rect.TopRight.X,
            bottom = rect.BottomRight.Y
        }, UnmanagedMethods.MONITOR.MONITOR_DEFAULTTONULL);

        return ScreenFromHMonitor(monitor);
    }

    public WinScreen? ScreenFromHMonitor(IntPtr hmonitor)
    {
        if (TryGetScreen(hmonitor, out var screen))
            return screen;

        return null;
    }

    public WinScreen? ScreenFromHwnd(IntPtr hwnd, MONITOR flags = MONITOR.MONITOR_DEFAULTTONULL)
    {
        var monitor = MonitorFromWindow(hwnd, flags);

        return ScreenFromHMonitor(monitor);
    }
}
