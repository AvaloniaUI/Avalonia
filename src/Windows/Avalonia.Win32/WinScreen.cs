using System;
using Avalonia.Platform;

namespace Avalonia.Win32
{
    public class WinScreen : Screen
    {
        private readonly IntPtr hMonitor;

        public WinScreen(Rect bounds, Rect workingArea, bool primary, IntPtr hMonitor) : base(bounds, workingArea, primary)
        {
            this.hMonitor = hMonitor;
        }

        public override int GetHashCode()
        {
            return (int)hMonitor;
        }

        public override bool Equals(object obj)
        {
            return (obj is WinScreen screen) ? this.hMonitor == screen.hMonitor : base.Equals(obj);
        }
    }
}