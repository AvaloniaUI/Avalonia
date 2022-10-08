using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using static Avalonia.X11.XLib;
using JetBrains.Annotations;

namespace Avalonia.X11
{
    class X11Screens : IScreenImpl
    {
        private IX11Screens _impl;

        public X11Screens(IX11Screens impl)
        {
            _impl = impl;
        }

        static unsafe X11Screen[] UpdateWorkArea(X11Info info, X11Screen[] screens)
        {
            var rect = default(PixelRect);
            foreach (var s in screens)
            {
                rect = rect.Union(s.Bounds);
                //Fallback value
                s.WorkingArea = s.Bounds;
            }

            var res = XGetWindowProperty(info.Display,
                info.RootWindow,
                info.Atoms._NET_WORKAREA,
                IntPtr.Zero, 
                new IntPtr(128),
                false,
                info.Atoms.AnyPropertyType,
                out var type,
                out var format,
                out var count,
                out var bytesAfter,
                out var prop);
            
            if (res != (int)Status.Success || type == IntPtr.Zero ||
                format == 0 || bytesAfter.ToInt64() != 0 || count.ToInt64() % 4 != 0)
                return screens;

            var pwa = (IntPtr*)prop;
            var wa = new PixelRect(pwa[0].ToInt32(), pwa[1].ToInt32(), pwa[2].ToInt32(), pwa[3].ToInt32());


            foreach (var s in screens)
            {
                s.WorkingArea = s.Bounds.Intersect(wa);
                if (s.WorkingArea.Width <= 0 || s.WorkingArea.Height <= 0)
                    s.WorkingArea = s.Bounds;
            }

            XFree(prop);
            return screens;
        }
        
        class Randr15ScreensImpl : IX11Screens
        {
            private readonly X11ScreensUserSettings _settings;
            private X11Screen[] _cache;
            private X11Info _x11;
            private IntPtr _window;
            const int EDIDStructureLength = 32; // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
            
            public Randr15ScreensImpl(AvaloniaX11Platform platform, X11ScreensUserSettings settings)
            {
                _settings = settings;
                _x11 = platform.Info;
                _window = CreateEventWindow(platform, OnEvent);
                XRRSelectInput(_x11.Display, _window, RandrEventMask.RRScreenChangeNotify);
            }

            private void OnEvent(ref XEvent ev)
            {
                // Invalidate cache on RRScreenChangeNotify
                if ((int)ev.type == _x11.RandrEventBase + (int)RandrEvent.RRScreenChangeNotify)
                    _cache = null;
            }

            private unsafe Size? GetPhysicalMonitorSizeFromEDID(IntPtr rrOutput)
            {
                if(rrOutput == IntPtr.Zero)
                    return null;
                var properties = XRRListOutputProperties(_x11.Display,rrOutput, out int propertyCount);
                var hasEDID = false;
                for(var pc = 0; pc < propertyCount; pc++)
                {
                    if(properties[pc] == _x11.Atoms.EDID)
                        hasEDID = true;
                }
                if(!hasEDID)
                    return null;
                XRRGetOutputProperty(_x11.Display, rrOutput, _x11.Atoms.EDID, 0, EDIDStructureLength, false, false, _x11.Atoms.AnyPropertyType, out IntPtr actualType, out int actualFormat, out int bytesAfter, out _, out IntPtr prop);
                if(actualType != _x11.Atoms.XA_INTEGER)
                    return null;
                if(actualFormat != 8) // Expecting an byte array
                    return null;

                var edid = new byte[bytesAfter];
                Marshal.Copy(prop,edid,0,bytesAfter);
                XFree(prop);
                XFree(new IntPtr(properties));
                if(edid.Length < 22)
                    return null;
                var width = edid[21]; // 0x15 1 Max. Horizontal Image Size cm. 
                var height = edid[22]; // 0x16 1 Max. Vertical Image Size cm. 
                if(width == 0 && height == 0)
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
                        double density = 0;
                        if (_settings.NamedScaleFactors?.TryGetValue(name, out density) != true)
                        {
                            for(int o = 0; o < mon.NOutput; o++)
                            {
                                var outputSize = GetPhysicalMonitorSizeFromEDID(mon.Outputs[o]);
                                var outputDensity = 1d;
                                if(outputSize != null)
                                    outputDensity = X11Screen.GuessPixelDensity(bounds, outputSize.Value);
                                if(density == 0 || density > outputDensity)
                                {
                                    density = outputDensity;
                                    pSize = outputSize;
                                }
                            }
                        }
                        if(density == 0)
                            density = 1;
                        density *= _settings.GlobalScaleFactor;
                        screens[c] = new X11Screen(bounds, mon.Primary != 0, name, pSize, density);
                    }
                    
                    XFree(new IntPtr(monitors));
                    _cache = UpdateWorkArea(_x11, screens);
                    return screens;
                }
            }
        }

        class FallbackScreensImpl : IX11Screens
        {
            public FallbackScreensImpl(X11Info info, X11ScreensUserSettings settings)
            {
                if (XGetGeometry(info.Display, info.RootWindow, out var geo))
                {

                    Screens = UpdateWorkArea(info,
                        new[]
                        {
                            new X11Screen(new PixelRect(0, 0, geo.width, geo.height), true, "Default", null,
                                settings.GlobalScaleFactor)
                        });
                }
                else
                {
                    Screens = new[]
                    {
                        new X11Screen(new PixelRect(0, 0, 1920, 1280), true, "Default", null,
                            settings.GlobalScaleFactor)
                    };
                }
            }

            public X11Screen[] Screens { get; }
        }
        
        public static IX11Screens Init(AvaloniaX11Platform platform)
        {
            var info = platform.Info;
            var settings = X11ScreensUserSettings.Detect();
            var impl = (info.RandrVersion != null && info.RandrVersion >= new Version(1, 5))
                ? new Randr15ScreensImpl(platform, settings)
                : (IX11Screens)new FallbackScreensImpl(info, settings);

            return impl;

        }

        public Screen ScreenFromPoint(PixelPoint point)
        {
            return ScreenHelper.ScreenFromPoint(point, AllScreens);
        }

        public Screen ScreenFromRect(PixelRect rect)
        {
            return ScreenHelper.ScreenFromRect(rect, AllScreens);
        }

        public Screen ScreenFromWindow(IWindowBaseImpl window)
        {
            return ScreenHelper.ScreenFromWindow(window, AllScreens);
        }

        public int ScreenCount => _impl.Screens.Length;

        public IReadOnlyList<Screen> AllScreens =>
            _impl.Screens.Select(s => new Screen(s.PixelDensity, s.Bounds, s.WorkingArea, s.IsPrimary)).ToArray();
    }

    interface IX11Screens
    {
        X11Screen[] Screens { get; }
    }

    class X11ScreensUserSettings
    {
        public double GlobalScaleFactor { get; set; } = 1;
        public Dictionary<string, double> NamedScaleFactors { get; set; }

        static double? TryParse(string s)
        {
            if (s == null)
                return null;
            if (double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var rv))
                return rv;
            return null;
        }
        

            public static X11ScreensUserSettings DetectEnvironment()
            {
                var globalFactor = Environment.GetEnvironmentVariable("AVALONIA_GLOBAL_SCALE_FACTOR");
                var screenFactors = Environment.GetEnvironmentVariable("AVALONIA_SCREEN_SCALE_FACTORS");
                if (globalFactor == null && screenFactors == null)
                    return null;

                var rv = new  X11ScreensUserSettings
                {
                    GlobalScaleFactor = TryParse(globalFactor) ?? 1 
                };

                try
                {
                    if (!string.IsNullOrWhiteSpace(screenFactors))
                    {
                        rv.NamedScaleFactors = screenFactors.Split(';').Where(x => !string.IsNullOrWhiteSpace(x))
                            .Select(x => x.Split('=')).ToDictionary(x => x[0],
                                x => double.Parse(x[1], CultureInfo.InvariantCulture));
                    }
                }
                catch
                {
                    //Ignore
                }

                return rv;  
            }


        public static X11ScreensUserSettings Detect()
        {
            return DetectEnvironment() ?? new X11ScreensUserSettings();
        }
    }

    class X11Screen
    {
        private const int FullHDWidth = 1920;
        private const int FullHDHeight = 1080;
        public bool IsPrimary { get; }
        public string Name { get; set; }
        public PixelRect Bounds { get; set; }
        public Size? PhysicalSize { get; set; }
        public double PixelDensity { get; set; }
        public PixelRect WorkingArea { get; set; }

        public X11Screen(PixelRect bounds, bool primary,
            string name, Size? physicalSize, double? pixelDensity)
        {
            IsPrimary = primary;
            Name = name;
            Bounds = bounds;
            if (physicalSize == null && pixelDensity == null)
            {
                PixelDensity = 1;
            }
            else if (pixelDensity == null)
            {
                PixelDensity = GuessPixelDensity(bounds, physicalSize.Value);
            }
            else
            {
                PixelDensity = pixelDensity.Value;
                PhysicalSize = physicalSize;
            }
        }

        public static double GuessPixelDensity(PixelRect pixel, Size physical)
        {
            var calculatedDensity = 1d;
            if(physical.Width > 0)
                calculatedDensity = pixel.Width <= FullHDWidth ? 1 : Math.Max(1, pixel.Width / physical.Width * 25.4 / 96);
            else if(physical.Height > 0)
                calculatedDensity = pixel.Height <= FullHDHeight ? 1 : Math.Max(1, pixel.Height / physical.Height * 25.4 / 96);
            
            if(calculatedDensity > 3)
                return 1;
            else
            {
                var sanePixelDensities = new double[] { 1, 1.25, 1.50, 1.75, 2 };
                foreach(var saneDensity in sanePixelDensities)
                {
                    if(calculatedDensity <= saneDensity + 0.20)
                        return saneDensity;
                }
                return sanePixelDensities.Last();
            }
        }
    }
}
