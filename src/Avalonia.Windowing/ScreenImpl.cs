using System;
using Avalonia.Platform;
using Avalonia.Windowing.Bindings;

namespace Avalonia.Windowing
{
    public class ScreenImpl : IScreenImpl
    {
        private EventsLoop _loop;
        private Screen[] _screens;

        public ScreenImpl(EventsLoop loop)
        {
            _loop = loop;
        }

        public int ScreenCount => (int)_loop.GetAvailableMonitors();

        private Screen[] _allScreens;
        public Screen[] AllScreens
        {
            get
            {
                if (_allScreens == null  || ScreenCount != _allScreens.Length)
                {
                    Screen[] screens = new Screen[ScreenCount];

                    for (UInt32 i = 0; i < ScreenCount; i++)
                    {
                        var monitor = _loop.GetMonitor(i);

                        var bounds = new Rect(new Point(monitor.Position.X, monitor.Position.Y), new Size(monitor.Size.Width, monitor.Size.Height));

                        // TODO winit doesnt support the concept of working area. (size minus area used by taskbar.

                        screens[i] = new Screen(bounds, bounds, monitor.IsPrimary == 1);
                    }

                    _allScreens = screens;
                }
                return _allScreens;
            }
        }}
}
