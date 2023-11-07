using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using static Avalonia.X11.XLib;

namespace Avalonia.X11.Screens
{
    internal partial class X11Screens : IScreenImpl
    {
        private IX11RawScreenInfoProvider _impl;
        private IScalingProvider _scaling;
        internal event Action Changed;

        public X11Screens(AvaloniaX11Platform platform)
        {
            var info = platform.Info;
            _impl = (info.RandrVersion != null && info.RandrVersion >= new Version(1, 5))
                ? new Randr15ScreensImpl(platform)
                : (IX11RawScreenInfoProvider)new FallbackScreensImpl(platform);
            _impl.Changed += () => Changed?.Invoke();
            _scaling = GetScalingProvider(platform);
            if (_scaling is IScalingProviderWithChanges scalingWithChanges)
                scalingWithChanges.SettingsChanged += () => Changed?.Invoke();
        }

        private static unsafe X11Screen[] UpdateWorkArea(X11Info info, X11Screen[] screens)
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

        public IReadOnlyList<Screen> AllScreens
        {
            get
            {
                var rawScreens = _impl.Screens;
                if (!rawScreens.Any(s => s.IsPrimary) && rawScreens.Length > 0)
                    rawScreens[0].IsPrimary = true;
                return rawScreens.Select((s, i) =>
                        new Screen(_scaling.GetScaling(s, i), s.Bounds, s.WorkingArea, s.IsPrimary))
                    .ToArray();
            }
        }
    }
}
