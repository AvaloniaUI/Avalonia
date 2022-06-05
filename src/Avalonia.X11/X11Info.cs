using System;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using static Avalonia.X11.XLib;
// ReSharper disable UnusedAutoPropertyAccessor.Local
namespace Avalonia.X11
{
    unsafe class X11Info
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
        
        public int XInputOpcode { get; }
        public int XInputEventBase { get; }
        public int XInputErrorBase { get; }
        
        public Version XInputVersion { get; }

        public IntPtr LastActivityTimestamp { get; set; }
        public XVisualInfo? TransparentVisualInfo { get; set; }
        public bool HasXim { get; set; }
        public bool HasXSync { get; set; }
        public IntPtr DefaultFontSet { get; set; }
        
        public unsafe X11Info(IntPtr display, IntPtr deferredDisplay, bool useXim)
        {
            Display = display;
            DeferredDisplay = deferredDisplay;
            DefaultScreen = XDefaultScreen(display);
            BlackPixel = XBlackPixel(display, DefaultScreen);
            RootWindow = XRootWindow(display, DefaultScreen);
            DefaultCursor = XCreateFontCursor(display, CursorFontShape.XC_left_ptr);
            DefaultRootWindow = XDefaultRootWindow(display);
            Atoms = new X11Atoms(display);

            DefaultFontSet = XCreateFontSet(Display, "-*-*-*-*-*-*-*-*-*-*-*-*-*-*",
                out var _, out var _, IntPtr.Zero);
            
            if (useXim)
            {
                XSetLocaleModifiers("");
                Xim = XOpenIM(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
                if (Xim != IntPtr.Zero)
                    HasXim = true;
            }

            if (Xim == IntPtr.Zero)
            {
                XSetLocaleModifiers("@im=none");
                Xim = XOpenIM(display, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);
            }

            XMatchVisualInfo(Display, DefaultScreen, 32, 4, out var visual);
            if (visual.depth == 32)
                TransparentVisualInfo = visual;
            
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
            
            try
            {
                if (XQueryExtension(display, "XInputExtension",
                        out var xiopcode, out var xievent, out var xierror))
                {
                    int major = 2, minor = 2;
                    if (XIQueryVersion(display, ref major, ref minor) == Status.Success)
                    {
                        XInputVersion = new Version(major, minor);
                        XInputOpcode = xiopcode;
                        XInputEventBase = xievent;
                        XInputErrorBase = xierror;
                    }
                }
            }
            catch
            {
                //Ignore, XI is not supported
            }

            try
            {
                HasXSync = XSyncInitialize(display, out _, out _) != Status.Success;
            }
            catch
            {
                //Ignore, XSync is not supported
            }
        }
    }
}
