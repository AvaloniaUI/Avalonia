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
                                                                                           screens[index] = new ScreenImpl(monitorInfo)
                                                                                                            {
                                                                                                                hMonitor = monitor
                                                                                                            };
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
        
        private IntPtr _hMonitor = IntPtr.Zero;
        private IntPtr hMonitor
        {
            get => _hMonitor;
            set
            {
                if (hMonitor == IntPtr.Zero)
                    _hMonitor = value;
            }   
        }

        public IScreenImpl PrimaryScreen
        {
            get
            {
                for (var i = 0; i < AllScreens.Length; i++)
                {
                    if (AllScreens[i].Primary)
                        return AllScreens[i];
                }

                return null;
            }
        }

        private ScreenImpl(MONITORINFO monitorInfo)
        {
            RECT bounds = monitorInfo.rcMonitor;
            RECT workingArea = monitorInfo.rcWork;
            this.Bounds = new Rect(bounds.left, bounds.top, bounds.right, bounds.bottom);
            this.WorkingArea = new Rect(workingArea.left, workingArea.top, workingArea.right, workingArea.bottom);
            this.Primary = monitorInfo.dwFlags == 1;
        }

        public ScreenImpl()
        {
            this.Bounds = PrimaryScreen.Bounds;
            this.WorkingArea = PrimaryScreen.WorkingArea;
            this.Primary = PrimaryScreen.Primary;
        }

        public override bool Equals(object obj)
        {
            return obj is ScreenImpl && this.hMonitor == ((ScreenImpl)obj).hMonitor;
        }

        public override int GetHashCode()
        {
            return (int)hMonitor;
        }
    }
}
