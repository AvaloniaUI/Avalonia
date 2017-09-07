// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;
#if NETSTANDARD
using Win32Exception = Avalonia.Win32.NetStandard.AvaloniaWin32Exception;
#endif

namespace Avalonia.Win32
{
    public class ScreenImpl : IScreenImpl
    {
        public int screenCount => UnmanagedMethods.GetSystemMetrics(SystemMetric.SM_CMONITORS);

        public IScreenImpl[] AllScreens
        {
            get
            {
                int index = 0;
                ScreenImpl[] screens = new ScreenImpl[screenCount];
                UnmanagedMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, (IntPtr monitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr data) =>
                {
                    MONITORINFO monitorInfo = new MONITORINFO();
                    if (UnmanagedMethods.GetMonitorInfo(monitor, monitorInfo))
                    {
                        screens[index] = new ScreenImpl(monitorInfo);
                        index++;
                    }
                    return true;
                }, IntPtr.Zero);
                return screens;
            }
        }

        public Rect Bounds { get; }

        public Rect WorkingArea { get; }

        public bool Primary { get; }

        public IScreenImpl PrimaryScreen
        {
            get
            {
                IntPtr hMonitor = UnmanagedMethods.MonitorFromWindow(IntPtr.Zero, MONITOR.MONITOR_DEFAULTTOPRIMARY);
                MONITORINFO monitorInfo = new MONITORINFO();
                if(GetMonitorInfo(hMonitor, monitorInfo))
                    return new ScreenImpl(monitorInfo);
                return null;
            }
        }

        internal ScreenImpl(MONITORINFO monitorInfo)
        {
            RECT bounds = monitorInfo.rcMonitor;
            RECT workingArea = monitorInfo.rcWork;
            this.Bounds = new Rect(bounds.left, bounds.top, bounds.right, bounds.bottom);
            this.WorkingArea = new Rect(workingArea.left, workingArea.top, workingArea.right, workingArea.bottom);
            this.Primary = monitorInfo.dwFlags == 1;
        }

        public ScreenImpl() { }
    }
}
