using System;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    public class WinScreen : Screen
    {
        private readonly IntPtr _hMonitor;

        public WinScreen(double pixelDensity, PixelRect bounds, PixelRect workingArea, bool primary, IntPtr hMonitor) : base(pixelDensity, bounds, workingArea, primary)
        {
            _hMonitor = hMonitor;
        }

        public IntPtr Handle => _hMonitor;

        public override int GetHashCode()
        {
            return (int)_hMonitor;
        }

        public override bool Equals(object obj)
        {
            return (obj is WinScreen screen) ? _hMonitor == screen._hMonitor : base.Equals(obj);
        }
    }
}
