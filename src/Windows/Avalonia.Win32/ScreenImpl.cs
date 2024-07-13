using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32;

internal unsafe class ScreenImpl : ScreensBaseImpl<nint, WinScreen>
{
    protected override int GetScreenCount() => GetSystemMetrics(SystemMetric.SM_CMONITORS);

    protected override IReadOnlyList<nint> GetAllScreenKeys()
    {
        var screens = new List<nint>();
        var gcHandle = GCHandle.Alloc(screens);
        try
        {
            EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, &EnumDisplayMonitorsCallback, (IntPtr)gcHandle);
        }
        finally
        {
            gcHandle.Free();
        }

        return screens;

        [UnmanagedCallersOnly(CallConvs = [typeof(CallConvCdecl)])]
        static int EnumDisplayMonitorsCallback(nint monitor, nint hdcMonitor, nint lprcMonitor, nint dwData)
        {
            if (GCHandle.FromIntPtr(dwData).Target is List<nint> screens)
            {
                screens.Add(monitor);
                return 1;
            }
            return 0;
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
