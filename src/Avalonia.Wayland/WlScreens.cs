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
        private readonly Dictionary<WlOutput, WlScreen> _wlOutputs = new();

        public int ScreenCount => AllScreens.Count;

        public IReadOnlyList<Screen> AllScreens => _wlScreens.Values.Select(ScreenForWlScreen).ToList();

        public WlScreens(AvaloniaWaylandPlatform platform)
        {
            _platform = platform;
            platform.WlRegistryHandler.GlobalAdded += OnGlobalAdded;
            _platform.WlRegistryHandler.GlobalRemoved += OnGlobalRemoved;
        }

        public Screen? ScreenFromWindow(IWindowBaseImpl window) => ScreenHelper.ScreenFromWindow(window, AllScreens);

        public Screen? ScreenFromPoint(PixelPoint point) => ScreenHelper.ScreenFromPoint(point, AllScreens);

        public Screen? ScreenFromRect(PixelRect rect) => ScreenHelper.ScreenFromRect(rect, AllScreens);

        public Screen ScreenFromOutput(WlOutput wlOutput) => ScreenForWlScreen(_wlOutputs[wlOutput]);

        private static Screen ScreenForWlScreen(WlScreen wlScreen) => new(wlScreen.PixelDensity, wlScreen.Bounds, wlScreen.WorkingArea, true);

        public void Dispose()
        {
            _platform.WlRegistryHandler.GlobalAdded -= OnGlobalAdded;
            _platform.WlRegistryHandler.GlobalRemoved -= OnGlobalRemoved;
            foreach (var wlScreen in _wlScreens.Values)
                wlScreen.Dispose();
        }

        private void OnGlobalAdded(WlRegistryHandler.GlobalInfo globalInfo)
        {
            if (globalInfo.Interface != WlOutput.InterfaceName) return;
            var wlOutput = _platform.WlRegistryHandler.Bind(WlOutput.BindFactory, WlOutput.InterfaceVersion, globalInfo);
            var wlScreen = new WlScreen(wlOutput);
            _wlScreens.Add(globalInfo.Name, wlScreen);
            _wlOutputs.Add(wlOutput, wlScreen);
        }

        private void OnGlobalRemoved(WlRegistryHandler.GlobalInfo globalInfo)
        {
            if (globalInfo.Interface != WlOutput.InterfaceName) return;
            if (!_wlScreens.TryGetValue(globalInfo.Name, out var wlScreen)) return;
            _wlScreens.Remove(globalInfo.Name);
            _wlOutputs.Remove(wlScreen.WlOutput);
            wlScreen.Dispose();
        }

        private sealed class WlScreen : WlOutput.IEvents, IDisposable
        {
            private int _x, _y, _width, _height;

            public double PixelDensity { get; private set; }

            public PixelRect Bounds => PixelRect.FromRect(new Rect(_x, _y, _width, _height), PixelDensity);

            public PixelRect WorkingArea => Bounds;

            public WlOutput WlOutput { get; }

            public WlScreen(WlOutput wlOutput)
            {
                WlOutput = wlOutput;
                wlOutput.Events = this;
            }

            void WlOutput.IEvents.OnGeometry(WlOutput eventSender, int x, int y, int physicalWidth, int physicalHeight, WlOutput.SubpixelEnum subpixel, string make, string model, WlOutput.TransformEnum transform)
            {
                _x = x;
                _y = y;
            }

            void WlOutput.IEvents.OnMode(WlOutput eventSender, WlOutput.ModeEnum flags, int width, int height, int refresh)
            {
                _width = width;
                _height = height;
            }

            void WlOutput.IEvents.OnScale(WlOutput eventSender, int factor)
            {
                PixelDensity = factor;
            }

            void WlOutput.IEvents.OnDone(WlOutput eventSender) { }

            public void Dispose()
            {
                WlOutput.Dispose();
            }

            public void OnName(WlOutput eventSender, string name)
            {
                throw new NotImplementedException();
            }

            public void OnDescription(WlOutput eventSender, string description)
            {
                throw new NotImplementedException();
            }
        }
    }
}
