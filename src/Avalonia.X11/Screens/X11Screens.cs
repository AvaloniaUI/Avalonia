using System;
using System.Collections.Generic;
using Avalonia.Platform;
using static Avalonia.X11.Screens.X11Screens;

namespace Avalonia.X11.Screens
{
    internal partial class X11Screens : ScreensBase<nint, X11Screen>
    {
        private readonly IX11RawScreenInfoProvider _impl;

        public X11Screens(AvaloniaX11Platform platform)
        {
            var info = platform.Info;
            _impl = (info.RandrVersion != null && info.RandrVersion >= new Version(1, 5))
                ? new Randr15ScreensImpl(platform)
                : (IX11RawScreenInfoProvider)new FallbackScreensImpl(platform);
            _impl.Changed += OnChanged;
        }

        protected override int GetScreenCount() => _impl.ScreenKeys.Length;

        protected override IReadOnlyList<nint> GetAllScreenKeys() => _impl.ScreenKeys;

        protected override X11Screen CreateScreenFromKey(nint key) => _impl.CreateScreenFromKey(key);

        protected override void ScreenChanged(X11Screen screen)
        {
            var handle = screen.TryGetPlatformHandle()?.Handle;
            if (handle != null)
            {
                screen.Refresh(_impl.GetMonitorInfoByKey(handle.Value));
            }
        }
    }
}
