using System;
using System.Runtime.InteropServices;

namespace Avalonia.Gtk3.Interop
{
    class CairoSurface : SafeHandle
    {
        public CairoSurface() : base(IntPtr.Zero, true)
        {
        }

        protected override bool ReleaseHandle()
        {
            Native.CairoSurfaceDestroy(handle);
            return true;
        }

        public override bool IsInvalid => handle == IntPtr.Zero;
    }
}
