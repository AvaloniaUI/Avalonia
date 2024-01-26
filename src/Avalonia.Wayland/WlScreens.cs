using System;
using System.Collections.Generic;
using Avalonia.Platform;
using NWayland.Protocols.Wayland;

namespace Avalonia.Wayland
{
    internal class WlScreens : IScreenImpl, IDisposable
    {
        private readonly AvaloniaWaylandPlatform _platform;
        private readonly Dictionary<uint, WlOutput> _wlOutputs = new();
        private readonly Dictionary<WlOutput, WlScreen> _wlScreens = new();
        private readonly Dictionary<WlSurface, WlWindow> _wlWindows = new();
        private readonly List<Screen> _screens = new();

        public WlScreens(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            platform.WlRegistryHandler.GlobalAdded += OnGlobalAdded;
            _platform.WlRegistryHandler.GlobalRemoved += OnGlobalRemoved;
        }

        public int ScreenCount => _screens.Count;

        public IReadOnlyList<Screen> AllScreens => _screens;

        public Screen? ScreenFromWindow(IWindowBaseImpl window) => (window as WlWindow)?.WlOutput is { } wlOutput ? _wlScreens[wlOutput].Screen : null;

        public Screen? ScreenFromPoint(PixelPoint point) => null;

        public Screen? ScreenFromRect(PixelRect rect) => null;

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
            _wlOutputs.Add(globalInfo.Name, wlOutput);
            var wlScreen = new WlScreen(wlOutput, _screens);
            _wlScreens.Add(wlOutput, wlScreen);
        }

        private void OnGlobalRemoved(WlRegistryHandler.GlobalInfo globalInfo)
        {
            if (globalInfo.Interface is not WlOutput.InterfaceName || !_wlOutputs.TryGetValue(globalInfo.Name, out var wlOutput) || !_wlScreens.TryGetValue(wlOutput, out var wlScreen))
                return;
            _wlScreens.Remove(wlOutput);
            wlScreen.Dispose();
        }

        internal sealed class WlScreen : WlOutput.IEvents, IDisposable
        {
            private readonly WlOutput _wlOutput;
            private readonly List<Screen> _screens;

            private PixelPoint _position;
            private PixelSize _size;
            private int _scaling;

            public WlScreen(WlOutput wlOutput, List<Screen> screens)
            {
                _wlOutput = wlOutput;
                _screens = screens;
                wlOutput.Events = this;
            }

            public Screen? Screen { get; private set; }

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

            public void OnDone(WlOutput eventSender)
            {
                if (Screen is not null)
                    _screens.Remove(Screen);
                Screen = new Screen(_scaling, new PixelRect(_position, _size), new PixelRect(_position, _size), false);
                _screens.Add(Screen);
            }

            public void Dispose()
            {
                if (Screen is not null)
                    _screens.Remove(Screen);
                _wlOutput.Dispose();
            }
        }
    }
}
