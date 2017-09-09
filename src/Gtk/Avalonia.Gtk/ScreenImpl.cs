using System;
using Avalonia.Platform;
using Gdk;
using Window = Gtk.Window;

namespace Avalonia.Gtk
{
    internal class ScreenImpl : IScreenImpl
    {
        private Window window;

        public int screenCount
        {
            get => window.Display.DefaultScreen.NMonitors;
        }
        public IScreenImpl[] AllScreens {
            get
            {
                IScreenImpl[] screens = new IScreenImpl[screenCount];
                var screen = window.Display.DefaultScreen;
                
                for (short i = 0; i < screens.Length; i++)
                {
                    Rectangle geometry = screen.GetMonitorGeometry(i);
                    Rect geometryRect = new Rect(geometry.X, geometry.Y, geometry.Width, geometry.Height);
                    ScreenImpl s = new ScreenImpl(geometryRect, geometryRect, false);
                    screens[i] = s;
                }

                return screens;
            }
        }

        public IScreenImpl PrimaryScreen
        {
            get => null;
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

        public ScreenImpl(Window window)
        {
            this.window = window;
        }
    }
}