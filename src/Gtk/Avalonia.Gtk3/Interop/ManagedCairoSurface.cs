using System;
using Avalonia.Platform;

namespace Avalonia.Gtk3.Interop
{
    class ManagedCairoSurface : IDisposable
    {
        public IntPtr Buffer { get; private set; }
        public CairoSurface Surface { get; private set; }
        public int Stride { get; private set; }
        private int _size;
        private IRuntimePlatform _plat;
        private IUnmanagedBlob _blob;

        public ManagedCairoSurface(int width, int height)
        {
            _plat = AvaloniaLocator.Current.GetService<IRuntimePlatform>();
            Stride = width * 4;
            _size = height * Stride;
            _blob = _plat.AllocBlob(_size * 2);
            Buffer = _blob.Address;
            Surface = Native.CairoImageSurfaceCreateForData(Buffer, 1, width, height, Stride);
        }
        
        public void Dispose()
        {
            
            if (Buffer != IntPtr.Zero)
            {
                Surface.Dispose();
                _blob.Dispose();
                Buffer = IntPtr.Zero;
            }
        }

    }
}
