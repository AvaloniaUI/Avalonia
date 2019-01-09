using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
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
        public IntPtr DefaultCursor { get; }
        public X11Atoms Atoms { get; }
        public IntPtr Xim { get; }
        
        public int RandrEventBase { get; }
        public int RandrErrorBase { get; }
        
        public Version RandrVersion { get; }
        

        public IntPtr LastActivityTimestamp { get; set; }
        
        public unsafe X11Info(IntPtr display, IntPtr deferredDisplay)
        {
            Display = display;
            DeferredDisplay = deferredDisplay;
            DefaultScreen = XDefaultScreen(display);
            BlackPixel = XBlackPixel(display, DefaultScreen);
            RootWindow = XRootWindow(display, DefaultScreen);
            DefaultCursor = XCreateFontCursor(display, CursorFontShape.XC_arrow);
            DefaultRootWindow = XDefaultRootWindow(display);
            Atoms = new X11Atoms(display);
            //TODO: Open an actual XIM once we get support for preedit in our textbox
            XSetLocaleModifiers("@im=none");
            Xim = XOpenIM(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

            try
            {
                if (XRRQueryExtension(display, out int randrEventBase, out var randrErrorBase) != 0)
                {
                    RandrEventBase = randrEventBase;
                    RandrErrorBase = randrErrorBase;
                    if (XRRQueryVersion(display, out var major, out var minor) != 0)
                        RandrVersion = new Version(major, minor);
                }
            }
            catch
            {
                //Ignore, randr is not supported
            }

        }
    }
}
