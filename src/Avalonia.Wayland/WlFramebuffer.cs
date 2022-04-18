using System;
using Avalonia.Platform;

namespace Avalonia.Wayland
{
    public class WlFramebuffer : ILockedFramebuffer
    {
        public void Dispose()
        {
            
        }

        public IntPtr Address { get; }
        public PixelSize Size { get; }
        public int RowBytes { get; }
        public Vector Dpi { get; }
        public PixelFormat Format { get; }
    }
}
