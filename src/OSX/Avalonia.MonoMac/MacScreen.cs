using System;
using Avalonia.Platform;

namespace Avalonia.MonoMac
{
    public class MacScreen : Screen
    {
        private readonly IntPtr handle;
        
        public MacScreen(Rect bounds, Rect workingArea, bool primary, IntPtr handle) : base(bounds, workingArea, primary)
        {
            this.handle = handle;
        }

        public override int GetHashCode()
        {
            return (int)handle;
        }

        public override bool Equals(object obj)
        {
            return (obj is MacScreen screen) ? this.handle == screen.handle : base.Equals(obj);
        }
    }
}