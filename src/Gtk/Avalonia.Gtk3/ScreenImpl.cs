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
                GtkScreen screen = Native.GdkDisplayGetScreen(display, 0);
                return Native.GdkScreenGetNMonitors(screen);
            }
        }

        public IScreenImpl[] AllScreens
        {
            get
            {
                if (allScreens == null)
                {
                    IntPtr display = Native.GdkGetDefaultDisplay();
                    GtkScreen screen = Native.GdkDisplayGetDefaultScreen(display);
                    short primary = Native.GdkScreenGetPrimaryMonitor(screen);
                    IScreenImpl[] screens = new IScreenImpl[screenCount];
                    for (short i = 0; i < screens.Length; i++)
                    {
                        GdkRectangle workArea = new GdkRectangle(), geometry = new GdkRectangle();
                        Native.GdkScreenGetMonitorGeometry(screen, i, ref geometry);
                        Native.GdkScreenGetMonitorWorkarea(screen, i, ref workArea);
                        Rect workAreaRect = new Rect(workArea.X, workArea.Y, workArea.Width, workArea.Height);
                        Rect geometryRect = new Rect(geometry.X, geometry.Y, geometry.Width, geometry.Height);
                        ScreenImpl s = new ScreenImpl(geometryRect, workAreaRect, i == primary) { screenId = i };
                        screens[i] = s;
                    }

                    allScreens = screens;
                    monitorsChangedSignal = Signal.Connect<Native.D.monitors_changed>(screen, "monitors-changed", MonitorsChanged);
                }

                return allScreens;
            }
        }

        public IScreenImpl PrimaryScreen
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

        public Rect Bounds { get; }
        public Rect WorkingArea { get; }
        public bool Primary { get; }

        private static IScreenImpl[] allScreens;
        private int _screenId = -1;
        private static IDisposable monitorsChangedSignal;

        private int screenId
        {
            get => _screenId;
            set
            {
                if (_screenId == -1)
                    _screenId = value;
            }
        }

        public ScreenImpl()
        {
            this.Bounds = PrimaryScreen.Bounds;
            this.WorkingArea = PrimaryScreen.WorkingArea;
            this.Primary = PrimaryScreen.Primary;
            this.screenId = ((ScreenImpl)PrimaryScreen).screenId;
        }

        public ScreenImpl(Rect bounds, Rect workingArea, bool primary)
        {
            this.Bounds = bounds;
            this.WorkingArea = workingArea;
            this.Primary = primary;
        }

        public override bool Equals(object obj)
        {
            return obj is ScreenImpl && this.screenId == ((ScreenImpl)obj).screenId;
        }

        public override int GetHashCode()
        {
            return this.screenId;
        }

        private unsafe void MonitorsChanged(IntPtr screen, IntPtr userData)
        {
            allScreens = null;
            monitorsChangedSignal.Dispose();
        }
    }
}