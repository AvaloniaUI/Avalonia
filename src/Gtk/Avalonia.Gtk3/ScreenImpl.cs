using System;
using Avalonia.Gtk3.Interop;
using Avalonia.Platform;

namespace Avalonia.Gtk3
{
    internal class ScreenImpl : IScreenImpl
    {
        public int screenCount
        {
            get
            {
                IntPtr display = Native.GdkGetDefaultDisplay();
                IntPtr screen = Native.GdkDisplayGetScreen(display, 0);
                return Native.GdkScreenGetNMonitors(screen);
            }
        }
        public IScreenImpl[] AllScreens {
            get
            {
                IntPtr display = Native.GdkGetDefaultDisplay();
                IntPtr screen = Native.GdkDisplayGetScreen(display, 0);
                short primary = Native.GdkScreenGetPrimaryMonitor(screen);
                IScreenImpl[] screens = new IScreenImpl[screenCount];
                for (short i = 0; i < screens.Length; i++)
                {
                    GdkRectangle workArea = new GdkRectangle(), geometry = new GdkRectangle();
                    Native.GdkScreenGetMonitorGeometry(screen, i, ref geometry);
                    Native.GdkScreenGetMonitorWorkarea(screen, i, ref workArea);
                    Rect workAreaRect = new Rect(workArea.X, workArea.Y, workArea.Width, workArea.Height);
                    Rect geometryRect = new Rect(geometry.X, geometry.Y, geometry.Width, geometry.Height);
                    ScreenImpl s = new ScreenImpl(geometryRect, workAreaRect, i == primary);
                    screens[i] = s;
                }

                return screens;
            }
        }

        public IScreenImpl PrimaryScreen
        {
            get
            {
                IntPtr display = Native.GdkGetDefaultDisplay();
                IntPtr screen = Native.GdkDisplayGetScreen(display, 0);
                short primary = Native.GdkScreenGetPrimaryMonitor(screen);
                GdkRectangle workArea = new GdkRectangle(), geometry = new GdkRectangle();
                Native.GdkScreenGetMonitorGeometry(screen, primary, ref geometry);
                Native.GdkScreenGetMonitorWorkarea(screen, primary, ref workArea);
                Rect workAreaRect = new Rect(workArea.X, workArea.Y, workArea.Width, workArea.Height);
                Rect geometryRect = new Rect(geometry.X, geometry.Y, geometry.Width, geometry.Height);
                ScreenImpl s = new ScreenImpl(geometryRect, workAreaRect, true);
                return s;
            }
        }

        public Rect Bounds { get; }
        public Rect WorkingArea { get; }
        public bool Primary { get; }

        public ScreenImpl()
        {
        }

        public ScreenImpl(Rect bounds, Rect workingArea, bool primary)
        {
            this.Bounds = bounds;
            this.WorkingArea = workingArea;
            this.Primary = primary;
        }
    }
}