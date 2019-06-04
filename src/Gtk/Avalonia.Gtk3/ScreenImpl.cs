using System;
using System.Collections.Generic;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    internal class ScreenImpl : IScreenImpl
    {
        public int ScreenCount
        {
            get => _allScreens.Length;
        }
        
        private Screen[] _allScreens;
        public IReadOnlyList<Screen> AllScreens
        {
            get
            {
                if (_allScreens == null)
                {
                    IntPtr display = Native.GdkGetDefaultDisplay();
                    GdkScreen screen = Native.GdkDisplayGetDefaultScreen(display);
                    short primary = Native.GdkScreenGetPrimaryMonitor(screen);
                    Screen[] screens = new Screen[Native.GdkScreenGetNMonitors(screen)];
                    for (short i = 0; i < screens.Length; i++)
                    {
                        GdkRectangle workArea = new GdkRectangle(), geometry = new GdkRectangle();
                        Native.GdkScreenGetMonitorGeometry(screen, i, ref geometry);
                        Native.GdkScreenGetMonitorWorkarea(screen, i, ref workArea);
                        PixelRect workAreaRect = new PixelRect(workArea.X, workArea.Y, workArea.Width, workArea.Height);
                        PixelRect geometryRect = new PixelRect(geometry.X, geometry.Y, geometry.Width, geometry.Height);
                        GtkScreen s = new GtkScreen(geometryRect, workAreaRect, i == primary, i);
                        screens[i] = s;
                    }

                    _allScreens = screens;
                }

                return _allScreens;
            }
        }

        public ScreenImpl()
        {
            IntPtr display = Native.GdkGetDefaultDisplay();
            GdkScreen screen = Native.GdkDisplayGetDefaultScreen(display);
            Signal.Connect<Native.D.monitors_changed>(screen, "monitors-changed", MonitorsChanged);
        }

        private unsafe void MonitorsChanged(IntPtr screen, IntPtr userData)
        {
            _allScreens = null;
        }
    }
}
