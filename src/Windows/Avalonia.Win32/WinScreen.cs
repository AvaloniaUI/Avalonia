using System;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    internal class WinScreen : Screen
    {
        private readonly IntPtr _hMonitor;

        public WinScreen(double scaling, PixelRect bounds, PixelRect workingArea, bool isPrimary, IntPtr hMonitor)
            : base(scaling, bounds, workingArea, isPrimary)
        {
            _hMonitor = hMonitor;
        }

        public IntPtr Handle => _hMonitor;

        /// <inheritdoc />
        public override int GetHashCode()
        {
            return _hMonitor.GetHashCode();
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            return obj is WinScreen screen && _hMonitor == screen._hMonitor;
        }
    }
}
