using System;
using System.Collections.Generic;
using System.Linq;

using Avalonia.Platform;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal class WlScreens : IScreenImpl, IDisposable
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly Dictionary<uint, WlScreen> _wlScreens = new();
        private readonly Dictionary<WlSurface, WlWindow> _wlWindows = new();

        public WlScreens(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            platform.WlRegistryHandler.GlobalAdded += OnGlobalAdded;
            _platform.WlRegistryHandler.GlobalRemoved += OnGlobalRemoved;
        }

        public int ScreenCount => _wlScreens.Count;

        public IReadOnlyList<Screen> AllScreens => _wlScreens.Select(x => x.Value.ToScreen()).ToArray();

        public Screen? ScreenFromWindow(IWindowBaseImpl window) => ScreenHelper.ScreenFromWindow(window, AllScreens);

        public Screen? ScreenFromPoint(PixelPoint point) => ScreenHelper.ScreenFromPoint(point, AllScreens);

        public Screen? ScreenFromRect(PixelRect rect) => ScreenHelper.ScreenFromRect(rect, AllScreens);

        public void Dispose()
        {
            _platform.WlRegistryHandler.GlobalAdded -= OnGlobalAdded;
            _platform.WlRegistryHandler.GlobalRemoved -= OnGlobalRemoved;
            foreach (var wlScreen in _wlScreens.Values)
                wlScreen.Dispose();
        }

        internal WlWindow? WindowFromSurface(WlSurface? wlSurface) => wlSurface is not null && _wlWindows.TryGetValue(wlSurface, out var wlWindow) ? wlWindow : null;

        internal void AddWindow(WlWindow window)
        {
            _wlWindows.Add(window.WlSurface, window);
        }

        internal void RemoveWindow(WlWindow window)
        {
            _platform.WlInputDevice.InvalidateFocus(window);
            _wlWindows.Remove(window.WlSurface);
        }

        private void OnGlobalAdded(WlRegistryHandler.GlobalInfo globalInfo)
        {
            if (globalInfo.Interface != WlOutput.InterfaceName)
                return;
            var wlOutput = _platform.WlRegistryHandler.BindRequiredInterface(WlOutput.BindFactory, WlOutput.InterfaceVersion, globalInfo);
            var wlScreen = new WlScreen(wlOutput);
            _wlScreens.Add(globalInfo.Name, wlScreen);
        }

        private void OnGlobalRemoved(WlRegistryHandler.GlobalInfo globalInfo)
        {
            if (globalInfo.Interface is not WlOutput.InterfaceName || !_wlScreens.TryGetValue(globalInfo.Name, out var wlScreen))
                return;
            _wlScreens.Remove(globalInfo.Name);
            wlScreen.Dispose();
        }

        internal sealed class WlScreen : WlOutput.IEvents, IDisposable
        {
            private PixelPoint _position;
            private PixelSize _size;
            private int _scaling;

            public WlScreen(WlOutput wlOutput)
            {
                WlOutput = wlOutput;
                wlOutput.Events = this;
            }

            public WlOutput WlOutput { get; }

            public void OnGeometry(WlOutput eventSender, int x, int y, int physicalWidth, int physicalHeight, WlOutput.SubpixelEnum subpixel, string make, string model, WlOutput.TransformEnum transform)
            {
                _position = new PixelPoint(x, y);
            }

            public void OnMode(WlOutput eventSender, WlOutput.ModeEnum flags, int width, int height, int refresh)
            {
                if (flags.HasAllFlags(WlOutput.ModeEnum.Current))
                    _size = new PixelSize(width, height);
            }

            public void OnScale(WlOutput eventSender, int factor)
            {
                _scaling = factor;
            }

            public void OnName(WlOutput eventSender, string name) { }

            public void OnDescription(WlOutput eventSender, string description) { }

            public void OnDone(WlOutput eventSender) { }

            public Screen ToScreen() => new(_scaling, new PixelRect(_position, _size), new PixelRect(_position, _size), false);

            public void Dispose() => WlOutput.Dispose();
        }
    }
}
