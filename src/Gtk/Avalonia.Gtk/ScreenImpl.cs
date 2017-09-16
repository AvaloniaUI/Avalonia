using System;
using Avalonia.Controls;
using Avalonia.Platform;
using Avalonia.Utilities;
using Gdk;
using Screen = Avalonia.Platform.Screen;
using Window = Gtk.Window;

namespace Avalonia.Gtk
{
    internal class ScreenImpl : IScreenImpl
    {
        private Window window;

        public int ScreenCount
        {
            get => window.Display.DefaultScreen.NMonitors;
        }
        public Screen[] AllScreens {
            get
            {
                Screen[] screens = new Screen[ScreenCount];
                var screen = window.Display.DefaultScreen;
                
                for (short i = 0; i < screens.Length; i++)
                {
                    Rectangle geometry = screen.GetMonitorGeometry(i);
                    Rect geometryRect = new Rect(geometry.X, geometry.Y, geometry.Width, geometry.Height);
                    Screen s = new Screen(geometryRect, geometryRect, false);
                    screens[i] = s;
                }

                return screens;
            }
        }

        public ScreenImpl(Window window)
        {
            this.window = window;
        }
    }
}