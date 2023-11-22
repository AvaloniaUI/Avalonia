
#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using static Avalonia.X11.XLib;
namespace Avalonia.X11.Screens;

internal partial class X11Screens
{
    internal class X11Screen
    {
        public bool IsPrimary { get; set; }
        public string Name { get; set; }
        public PixelRect Bounds { get; set; }
        public Size? PhysicalSize { get; set; }
        public PixelRect WorkingArea { get; set; }

        public X11Screen(
            PixelRect bounds,
            bool isPrimary,
            string name,
            Size? physicalSize)
        {
            IsPrimary = isPrimary;
            Name = name;
            Bounds = bounds;
            PhysicalSize = physicalSize;
        }
    }

    internal interface IX11RawScreenInfoProvider
    {
        X11Screen[] Screens { get; }
        event Action? Changed;
    }

    
    private class Randr15ScreensImpl : IX11RawScreenInfoProvider
    {
        private X11Screen[]? _cache;
        private readonly X11Info _x11;
        private readonly IntPtr _window;

        // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
        private const int EDIDStructureLength = 32; 

        public event Action? Changed;
        
        public Randr15ScreensImpl(AvaloniaX11Platform platform)
        {
            _x11 = platform.Info;
            _window = CreateEventWindow(platform, OnEvent);
            XRRSelectInput(_x11.Display, _window, RandrEventMask.RRScreenChangeNotify);
        }

        private void OnEvent(ref XEvent ev)
        {
            if ((int)ev.type == _x11.RandrEventBase + (int)RandrEvent.RRScreenChangeNotify)
            {
                _cache = null;
                Changed?.Invoke();
            }
        }

        private unsafe Size? GetPhysicalMonitorSizeFromEDID(IntPtr rrOutput)
        {
            if (rrOutput == IntPtr.Zero)
                return null;
            var properties = XRRListOutputProperties(_x11.Display, rrOutput, out int propertyCount);
            var hasEDID = false;
            for (var pc = 0; pc < propertyCount; pc++)
            {
                if (properties[pc] == _x11.Atoms.EDID)
                    hasEDID = true;
            }

            if (!hasEDID)
                return null;
            XRRGetOutputProperty(_x11.Display, rrOutput, _x11.Atoms.EDID, 0, EDIDStructureLength, false, false,
                _x11.Atoms.AnyPropertyType, out IntPtr actualType, out int actualFormat, out int bytesAfter, out _,
                out IntPtr prop);
            if (actualType != _x11.Atoms.XA_INTEGER)
                return null;
            if (actualFormat != 8) // Expecting an byte array
                return null;

            var edid = new byte[bytesAfter];
            Marshal.Copy(prop, edid, 0, bytesAfter);
            XFree(prop);
            XFree(new IntPtr(properties));
            if (edid.Length < 22)
                return null;
            var width = edid[21]; // 0x15 1 Max. Horizontal Image Size cm. 
            var height = edid[22]; // 0x16 1 Max. Vertical Image Size cm. 
            if (width == 0 && height == 0)
                return null;
            return new Size(width * 10, height * 10);
        }

        public unsafe X11Screen[] Screens
        {
            get
            {
                if (_cache != null)
                    return _cache;
                var monitors = XRRGetMonitors(_x11.Display, _window, true, out var count);

                var screens = new X11Screen[count];
                for (var c = 0; c < count; c++)
                {
                    var mon = monitors[c];
                    var namePtr = XGetAtomName(_x11.Display, mon.Name);
                    var name = Marshal.PtrToStringAnsi(namePtr);
                    XFree(namePtr);
                    var bounds = new PixelRect(mon.X, mon.Y, mon.Width, mon.Height);
                    Size? pSize = null;

                    for (int o = 0; o < mon.NOutput; o++)
                    {
                        var outputSize = GetPhysicalMonitorSizeFromEDID(mon.Outputs[o]);
                        if (outputSize != null)
                        {
                            pSize = outputSize;
                            break;
                        }
                    }

                    screens[c] = new X11Screen(bounds, mon.Primary != 0, name ?? string.Empty, pSize);
                }

                XFree(new IntPtr(monitors));
                _cache = UpdateWorkArea(_x11, screens);
                return screens;
            }
        }
    }

    private class FallbackScreensImpl : IX11RawScreenInfoProvider
    {
        private readonly X11Info _info;

        public event Action? Changed
        {
            add { }
            remove { }
        }

        public FallbackScreensImpl(AvaloniaX11Platform platform)
        {
            _info = platform.Info;
            if (UpdateRootWindowGeometry())
                platform.Globals.RootGeometryChangedChanged += () => UpdateRootWindowGeometry();
        }

        bool UpdateRootWindowGeometry()
        {
            var res = XGetGeometry(_info.Display, _info.RootWindow, out var geo);
            if(res)
            {
                Screens = UpdateWorkArea(_info,
                    new[]
                    {
                        new X11Screen(new PixelRect(0, 0, geo.width, geo.height), true, "Default", null)
                    });
            }

            return res;
        }

        public X11Screen[] Screens { get; private set; } = new[]
            { new X11Screen(new PixelRect(0, 0, 1920, 1280), true, "Default", null) };
    }
}
