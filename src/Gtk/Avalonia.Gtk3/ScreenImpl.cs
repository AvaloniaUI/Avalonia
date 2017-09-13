using System;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    internal class ScreenImpl : IScreenImpl
    {
        public int ScreenCount
        {
            get => allScreens.Length;
        }

        public Screen[] AllScreens
        {
            get
            {
                if (allScreens == null)
                {
                    IntPtr display = Native.GdkGetDefaultDisplay();
                    GdkScreen screen = Native.GdkDisplayGetDefaultScreen(display);
                    short primary = Native.GdkScreenGetPrimaryMonitor(screen);
                    Screen[] screens = new Screen[ScreenCount];
                    for (short i = 0; i < screens.Length; i++)
                    {
                        GdkRectangle workArea = new GdkRectangle(), geometry = new GdkRectangle();
                        Native.GdkScreenGetMonitorGeometry(screen, i, ref geometry);
                        Native.GdkScreenGetMonitorWorkarea(screen, i, ref workArea);
                        Rect workAreaRect = new Rect(workArea.X, workArea.Y, workArea.Width, workArea.Height);
                        Rect geometryRect = new Rect(geometry.X, geometry.Y, geometry.Width, geometry.Height);
                        GtkScreen s = new GtkScreen(geometryRect, workAreaRect, i == primary, i);
                        screens[i] = s;
                    }

                    allScreens = screens;
                    monitorsChangedSignal = Signal.Connect<Native.D.monitors_changed>(screen, "monitors-changed", MonitorsChanged);
                }

                return allScreens;
            }
        }

        public Screen PrimaryScreen
        {
            get
            {
                for (int i = 0; i < AllScreens.Length; i++)
                {
                    if (AllScreens[i].Primary)
                        return AllScreens[i];
                }

                return null;
            }
        }
        
        private static Screen[] allScreens;
        private static IDisposable monitorsChangedSignal;

        public ScreenImpl()
        {
        }

        private unsafe void MonitorsChanged(IntPtr screen, IntPtr userData)
        {
            allScreens = null;
            monitorsChangedSignal.Dispose();
        }
    }
}