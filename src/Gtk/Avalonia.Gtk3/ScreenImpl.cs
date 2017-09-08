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
                return Native.GdkDisplayGetNMonitors(display);
            }
        }
        public IScreenImpl[] AllScreens { get; }

        public IScreenImpl PrimaryScreen
        {
            get { return null; }
        }

        public Rect Bounds { get; }
        public Rect WorkingArea { get; }
        public bool Primary { get; }
    }
}