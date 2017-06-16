using System;

namespace Avalonia.Platform
{
    public interface ILockedFramebuffer : IDisposable
    {
        /// <summary>
        /// Address of the first pixel
        /// </summary>
        IntPtr Address { get; }

        /// <summary>
        /// Framebuffer width
        /// </summary>
        int Width { get; }
        
        /// <summary>
        /// Framebuffer height
        /// </summary>
        int Height { get; }
        
        /// <summary>
        /// Number of bytes per row
        /// </summary>
        int RowBytes { get; }
        
        /// <summary>
        /// DPI of underling screen
        /// </summary>
        Vector Dpi { get; }
        
        /// <summary>
        /// Pixel format
        /// </summary>
        PixelFormat Format { get; }
    }
}
