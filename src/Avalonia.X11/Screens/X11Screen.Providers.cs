using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Threading;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Screens;

internal partial class X11Screens
{
    internal unsafe class X11Screen(MonitorInfo info, X11Info x11, IScalingProvider? scalingProvider, int id) : PlatformScreen(new PlatformHandle(info.Name, "XRandRMonitorName"))
    {
        public Size? PhysicalSize { get; set; }

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
            PhysicalSize = info.PhysicalSize;
            UpdateWorkArea();
            Scaling = scalingProvider.GetScaling(this, id);
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
        X11Screen CreateScreenFromKey(nint key);
    }
    
    internal interface IX11RawScreenInfoProviderWithRefreshRate : IX11RawScreenInfoProvider
    {
        int MaxRefreshRate { get; }
    }

    internal unsafe struct MonitorInfo
    {
        public IntPtr Name;
        public bool IsPrimary;
        public int X;
        public int Y;
        public int Width;
        public int Height;
        public int RefreshRate;
        public Size? PhysicalSize;
        public int SharedRefreshRate;
    }

    private class Randr15ScreensImpl : IX11RawScreenInfoProviderWithRefreshRate
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
            XRRSelectInput(_x11.Display, _window,
                RandrEventMask.RRScreenChangeNotify
                | RandrEventMask.RROutputChangeNotifyMask
                | RandrEventMask.RROutputPropertyNotifyMask
                | RandrEventMask.RRCrtcChangeNotifyMask);
            
            if (_scalingProvider is IScalingProviderWithChanges scalingWithChanges)
                scalingWithChanges.SettingsChanged += () => Changed?.Invoke();
        }

        private void OnEvent(ref XEvent ev)
        {
            if (((int)ev.type - _x11.RandrEventBase) is  (int)RandrEvent.RRScreenChangeNotify or (int)RandrEvent.RRNotify)
            {
                _cache = null;
                // Delay triggering the update event
                Dispatcher.UIThread.Post(() => Changed?.Invoke(), DispatcherPriority.Normal);
            }
        }

        private unsafe MonitorInfo[] MonitorInfos
        {
            get
            {
                if (_cache != null)
                    return _cache;
                var monitors = XRRGetMonitors(_x11.Display, _window, true, out var count);
                var resources = XRRGetScreenResources(_x11.Display, _window);

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
                        PhysicalSize = GetPhysicalMonitorSizeFromFirstEligibleOutput(outputs),
                        SharedRefreshRate = GetSharedRefreshRateForOutputs(resources, outputs)
                    };
                }

                XRRFreeScreenResources(resources);
                XRRFreeMonitors(monitors);

                return _cache = screens;
            }
        }

        private unsafe int GetSharedRefreshRateForOutputs(XRRScreenResources* resources, IntPtr[] outputs)
        {
            int? minRate = null;
            foreach (var output in outputs)
            {
                var rate = GetRefreshRateForOutput(resources, output);
                if (rate.HasValue)
                    minRate = minRate.HasValue ? Math.Min(minRate.Value, rate.Value) : rate;
            }

            return minRate ?? 60;
        }

        private unsafe int? GetRefreshRateForOutput(XRRScreenResources* resources, IntPtr output)
        {
            // Check if output exists in resources
            var foundOutput = false;
            for (var c = 0; c < resources->noutput; c++)
            {
                if (resources->outputs[c] == output)
                {
                    foundOutput = true;
                    break;
                }
            }

            if (!foundOutput)
                return null;

            var outputInfo = XRRGetOutputInfo(_x11.Display, resources, output);
            if (outputInfo == null)
                return null;
            try
            {
                if (outputInfo->crtc == IntPtr.Zero)
                    return null;
                var crtc = XRRGetCrtcInfo(_x11.Display, resources, outputInfo->crtc);
                if (crtc == null)
                    return null;
                try
                {
                    if (crtc->mode == IntPtr.Zero)
                        return null;
                    for (var c = 0; c < resources->nmode; c++)
                    {
                        var mode = resources->modes[c];
                        if (mode.id == crtc->mode)
                        {
                            var multiplier = 1d;
                            if (mode.modeFlags.HasAnyFlag(RRModeFlags.RR_Interlace))
                                multiplier *= 2;
                            if (mode.modeFlags.HasAnyFlag(RRModeFlags.RR_DoubleScan))
                                multiplier /= 2;
                            if (mode.hTotal == 0 || mode.vTotal == 0 || mode.dotClock == 0)
                                return null;
                            var hz = mode.dotClock / ((double)mode.hTotal * mode.vTotal) * multiplier;
                            return (int)Math.Round(hz, MidpointRounding.ToEven);
                        }
                    }
                }
                finally
                {
                    XRRFreeCrtcInfo(crtc);
                }
            }
            finally
            {
                XRRFreeOutputInfo(outputInfo);
            }

            return null;
        }
        
        private Size? GetPhysicalMonitorSizeFromFirstEligibleOutput(IntPtr[] outputs)
        {
            Size? pSize = null;
            for (int o = 0; o < outputs.Length; o++)
            {
                var outputSize = GetPhysicalMonitorSizeFromEDID(outputs[o]);
                if (outputSize != null)
                {
                    pSize = outputSize;
                    break;
                }
            }

            return pSize;
        }
        
        private unsafe Size? GetPhysicalMonitorSizeFromEDID(IntPtr rrOutput)
        {
            if (rrOutput == IntPtr.Zero)
                return null;
            var properties = XRRListOutputPropertiesAsArray(_x11.Display, rrOutput);
            var hasEDID = false;
            for (var pc = 0; pc < properties.Length; pc++)
            {
                if (properties[pc] == _x11.Atoms.EDID)
                    hasEDID = true;
            }

            if (!hasEDID)
                return null;
            
            // Length of a EDID-Block-Length(128 bytes), XRRGetOutputProperty multiplies offset and length by 4
            const int EDIDStructureLength = 32;
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
            if (edid.Length < 22)
                return null;
            var width = edid[21]; // 0x15 1 Max. Horizontal Image Size cm. 
            var height = edid[22]; // 0x16 1 Max. Vertical Image Size cm. 
            if (width == 0 && height == 0)
                return null;
            return new Size(width * 10, height * 10);
        }

        public int MaxRefreshRate
        {
            get
            {
                var monitors = MonitorInfos;
                return monitors.Length == 0 ? 60 : monitors.Max(x => x.SharedRefreshRate);
            }
        }

        public X11Screen CreateScreenFromKey(nint key)
        {
            var infos = MonitorInfos;
            for (var i = 0; i < infos.Length; i++)
            {
                if (infos[i].Name == key)
                {
                    return new X11Screen(infos[i], _x11, _scalingProvider, i);
                }
            }

            throw new ArgumentOutOfRangeException(nameof(key));
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

        public X11Screen CreateScreenFromKey(nint key)
        {
            return new FallBackScreen(new PixelRect(0, 0, _geo.width, _geo.height), _info);
        }

        public nint[] ScreenKeys => new[] { IntPtr.Zero };
    }
}
