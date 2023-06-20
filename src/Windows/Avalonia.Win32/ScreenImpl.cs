using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Metadata;
using Avalonia.Platform;
using static Avalonia.Win32.Interop.UnmanagedMethods;

namespace Avalonia.Win32
{
    internal class ScreenImpl : IScreenImpl
    {
        private Screen[]? _allScreens;

        /// <inheritdoc />
        public int ScreenCount
        {
            get => GetSystemMetrics(SystemMetric.SM_CMONITORS);
        }

        /// <inheritdoc />
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
                                    dpi = x;
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
                                PixelRect avaloniaBounds = bounds.ToPixelRect();
                                PixelRect avaloniaWorkArea = workingArea.ToPixelRect();
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

        /// <inheritdoc />
        public Screen? ScreenFromWindow(IWindowBaseImpl window)
        {
            var handle = window.Handle.Handle;

            var monitor = MonitorFromWindow(handle, MONITOR.MONITOR_DEFAULTTONULL);

            return FindScreenByHandle(monitor);
        }

        /// <inheritdoc />
        public Screen? ScreenFromPoint(PixelPoint point)
        {
            var monitor = MonitorFromPoint(new POINT
            {
                X = point.X,
                Y = point.Y
            }, MONITOR.MONITOR_DEFAULTTONULL);

            return FindScreenByHandle(monitor);
        }

        /// <inheritdoc />
        public Screen? ScreenFromRect(PixelRect rect)
        {
            var monitor = MonitorFromRect(new RECT
            {
                left = rect.TopLeft.X,
                top = rect.TopLeft.Y,
                right = rect.TopRight.X,
                bottom = rect.BottomRight.Y
            }, MONITOR.MONITOR_DEFAULTTONULL);

            return FindScreenByHandle(monitor);
        }

        private Screen? FindScreenByHandle(IntPtr handle)
        {
            return AllScreens.Cast<WinScreen>().FirstOrDefault(m => m.Handle == handle);
        }
    }
}
