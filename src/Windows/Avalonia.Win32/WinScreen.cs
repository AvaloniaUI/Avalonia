using System;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    public class WinScreen : Screen
    {
        private readonly IntPtr _hMonitor;

        public WinScreen(double scaling, PixelRect bounds, PixelRect workingArea, bool isPrimary, IntPtr hMonitor)
            : base(scaling, bounds, workingArea, isPrimary)
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
