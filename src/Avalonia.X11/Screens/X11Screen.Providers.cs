
#nullable enable
using System;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using static Avalonia.X11.XLib;
namespace Avalonia.X11.Screens;

internal partial class X11Screens
{
    internal unsafe class X11Screen(MonitorInfo info, X11Info x11, IScalingProvider? scalingProvider, int id) : PlatformScreen(new PlatformHandle(info.Name, "XRandRMonitorName"))
    {
        public Size? PhysicalSize { get; set; }
        // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
        private const int EDIDStructureLength = 32;

        public virtual void Refresh()
        {
            if (scalingProvider == null)
                return;

            var namePtr = XGetAtomName(x11.Display, info.Name);
            var name = Marshal.PtrToStringAnsi(namePtr);
            XFree(namePtr);
            DisplayName = name;
            IsPrimary = info.IsPrimary;
            Bounds = new PixelRect(info.X, info.Y, info.Width, info.Height);

            Size? pSize = null;
            for (int o = 0; o < info.Outputs.Length; o++)
            {
                var outputSize = GetPhysicalMonitorSizeFromEDID(info.Outputs[o]);
                if (outputSize != null)
                {
                    pSize = outputSize;
                    break;
                }
            }
            PhysicalSize = pSize;
            UpdateWorkArea();
            Scaling = scalingProvider.GetScaling(this, id);
        }

        private unsafe Size? GetPhysicalMonitorSizeFromEDID(IntPtr rrOutput)
        {
            if (rrOutput == IntPtr.Zero || x11 == null)
                return null;
            var properties = XRRListOutputProperties(x11.Display, rrOutput, out int propertyCount);
            var hasEDID = false;
            for (var pc = 0; pc < propertyCount; pc++)
            {
                if (properties[pc] == x11.Atoms.EDID)
                    hasEDID = true;
            }

            if (!hasEDID)
                return null;
            XRRGetOutputProperty(x11.Display, rrOutput, x11.Atoms.EDID, 0, EDIDStructureLength, false, false,
                x11.Atoms.AnyPropertyType, out IntPtr actualType, out int actualFormat, out int bytesAfter, out _,
                out IntPtr prop);
            if (actualType != x11.Atoms.XA_INTEGER)
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

        protected unsafe void UpdateWorkArea()
        {
            var rect = default(PixelRect);
            //Fallback value
            rect = rect.Union(Bounds);
            WorkingArea = Bounds;

            var res = XGetWindowProperty(x11.Display,
                x11.RootWindow,
                x11.Atoms._NET_WORKAREA,
                IntPtr.Zero,
                new IntPtr(128),
                false,
                x11.Atoms.AnyPropertyType,
                out var type,
                out var format,
                out var count,
                out var bytesAfter,
                out var prop);

            if (res != (int)Status.Success || type == IntPtr.Zero ||
                format == 0 || bytesAfter.ToInt64() != 0 || count.ToInt64() % 4 != 0)
                return;

            var pwa = (IntPtr*)prop;
            var wa = new PixelRect(pwa[0].ToInt32(), pwa[1].ToInt32(), pwa[2].ToInt32(), pwa[3].ToInt32());

            WorkingArea = Bounds.Intersect(wa);
            if (WorkingArea.Width <= 0 || WorkingArea.Height <= 0)
                WorkingArea = Bounds;
            XFree(prop);
        }
    }

    internal class FallBackScreen : X11Screen
    {
        public FallBackScreen(PixelRect pixelRect, X11Info x11) : base(default, x11, null, 0)
        {
            Bounds = pixelRect;
            DisplayName = "Default";
            IsPrimary = true;
            PhysicalSize = pixelRect.Size.ToSize(Scaling);
            UpdateWorkArea();
        }
        public override void Refresh()
        {
        }
    }

    internal interface IX11RawScreenInfoProvider
    {
        nint[] ScreenKeys { get; }
        event Action? Changed;
        X11Screen? CreateScreenFromKey(nint key);
    }

    internal unsafe struct MonitorInfo
    {
        public IntPtr Name;
        public bool IsPrimary;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public IntPtr[] Outputs;
    }

    private class Randr15ScreensImpl : IX11RawScreenInfoProvider
    {
        private MonitorInfo[]? _cache;
        private readonly X11Info _x11;
        private readonly IntPtr _window;
        private readonly IScalingProvider _scalingProvider;

        public nint[] ScreenKeys => MonitorInfos.Select(x => x.Name).ToArray();

        public event Action? Changed;

        public Randr15ScreensImpl(AvaloniaX11Platform platform)
        {
            _x11 = platform.Info;
            _window = CreateEventWindow(platform, OnEvent);
            _scalingProvider = GetScalingProvider(platform);
            XRRSelectInput(_x11.Display, _window, RandrEventMask.RRScreenChangeNotify);
            if (_scalingProvider is IScalingProviderWithChanges scalingWithChanges)
                scalingWithChanges.SettingsChanged += () => Changed?.Invoke();
        }

        private void OnEvent(ref XEvent ev)
        {
            if ((int)ev.type == _x11.RandrEventBase + (int)RandrEvent.RRScreenChangeNotify)
            {
                _cache = null;
                Changed?.Invoke();
            }
        }

        private unsafe MonitorInfo[] MonitorInfos
        {
            get
            {
                if (_cache != null)
                    return _cache;
                var monitors = XRRGetMonitors(_x11.Display, _window, true, out var count);

                var screens = new MonitorInfo[count];
                for (var c = 0; c < count; c++)
                {
                    var mon = monitors[c];
                    var outputs = new nint[mon.NOutput];

                    for (int i = 0; i < outputs.Length; i++)
                    {
                        outputs[i] = mon.Outputs[i];
                    }

                    screens[c] = new MonitorInfo()
                    {
                        Name = mon.Name,
                        IsPrimary = mon.Primary != 0,
                        X = mon.X,
                        Y = mon.Y,
                        Width = mon.Width,
                        Height = mon.Height,
                        Outputs = outputs
                    };
                }

                XFree(new IntPtr(monitors));

                return screens;
            }
        }

        public X11Screen? CreateScreenFromKey(nint key)
        {
            var info = MonitorInfos.Where(x => x.Name == key).FirstOrDefault();

            var infos = MonitorInfos;
            for (var i = 0; i < infos.Length; i++)
            {
                if (infos[i].Name == key)
                {
                    return new X11Screen(info, _x11, _scalingProvider, i);
                }
            }

            return null;
        }
    }

    private class FallbackScreensImpl : IX11RawScreenInfoProvider
    {
        private readonly X11Info _info;
        private XGeometry _geo;

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

        private bool UpdateRootWindowGeometry() => XGetGeometry(_info.Display, _info.RootWindow, out _geo);

        public X11Screen? CreateScreenFromKey(nint key)
        {
            return new FallBackScreen(new PixelRect(0, 0, _geo.width, _geo.height), _info);
        }

        public nint[] ScreenKeys => new[] { IntPtr.Zero };
    }
}
