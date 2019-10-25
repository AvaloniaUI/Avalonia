// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.Collections.Generic;
using Avalonia.Platform;
using Avalonia.Win32.Interop;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    public class ScreenImpl : IScreenImpl
    {
        public  int ScreenCount
        {
            get => GetSystemMetrics(SystemMetric.SM_CMONITORS);
        }

        private Screen[] _allScreens;
        public IReadOnlyList<Screen> AllScreens
        {
            get
            {
                if (_allScreens == null)
                {
                    int index = 0;
                    Screen[] screens = new Screen[ScreenCount];
                    EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero,
                        (IntPtr monitor, IntPtr hdcMonitor, ref Rect lprcMonitor, IntPtr data) =>
                        {
                            MONITORINFO monitorInfo = MONITORINFO.Create();
                            if (GetMonitorInfo(monitor, ref monitorInfo))
                            {
                                var dpi = 1.0;

                                var shcore = LoadLibrary("shcore.dll");
                                var method = GetProcAddress(shcore, nameof(GetDpiForMonitor));
                                if (method != IntPtr.Zero)
                                { 
                                    GetDpiForMonitor(monitor, MONITOR_DPI_TYPE.MDT_EFFECTIVE_DPI, out var x, out _);
                                    dpi = (double)x;
                                }
                                else
                                {
                                    var hdc = GetDC(IntPtr.Zero);

                                    double virtW = GetDeviceCaps(hdc, DEVICECAP.HORZRES);
                                    double physW = GetDeviceCaps(hdc, DEVICECAP.DESKTOPHORZRES);

                                    dpi = (96d * physW / virtW);

                                    ReleaseDC(IntPtr.Zero, hdc);
                                }

                                RECT bounds = monitorInfo.rcMonitor;
                                RECT workingArea = monitorInfo.rcWork;
                                PixelRect avaloniaBounds = new PixelRect(bounds.left, bounds.top, bounds.right - bounds.left,
                                    bounds.bottom - bounds.top);
                                PixelRect avaloniaWorkArea =
                                    new PixelRect(workingArea.left, workingArea.top, workingArea.right - workingArea.left,
                                        workingArea.bottom - workingArea.top);
                                screens[index] =
                                    new WinScreen(dpi / 96.0d, avaloniaBounds, avaloniaWorkArea, monitorInfo.dwFlags == 1,
                                        monitor);
                                index++;
                            }
                            return true;
                        }, IntPtr.Zero);
                    _allScreens = screens;
                }
                return _allScreens;
            }
        }

        public void InvalidateScreensCache()
        {
            _allScreens = null;
        }
    }
}
