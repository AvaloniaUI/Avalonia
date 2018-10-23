using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using static Avalonia.X11.XLib;
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Avalonia.X11
{
    class X11Info
    {
        public IntPtr Display { get; }
        public IntPtr DeferredDisplay { get; }
        public int DefaultScreen { get; }
        public IntPtr BlackPixel { get; }
        public IntPtr RootWindow { get; }
        public IntPtr DefaultRootWindow { get; }
        public XVisualInfo MatchedVisual { get; }
        public X11Atoms Atoms { get; }

        public IntPtr LastActivityTimestamp { get; set; }
        
        public unsafe X11Info(IntPtr display, IntPtr deferredDisplay)
        {
            Display = display;
            DeferredDisplay = deferredDisplay;
            DefaultScreen = XDefaultScreen(display);
            BlackPixel = XBlackPixel(display, DefaultScreen);
            RootWindow = XRootWindow(display, DefaultScreen);
            DefaultRootWindow = XDefaultRootWindow(display);
            Atoms = new X11Atoms(display);
            XMatchVisualInfo(display, DefaultScreen, 32, 4, out var info);
            MatchedVisual = info;
        }
    }
}
