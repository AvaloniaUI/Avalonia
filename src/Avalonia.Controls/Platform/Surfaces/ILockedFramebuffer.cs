using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Platform.Surfaces
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
        Size Dpi { get; }
        /// <summary>
        /// Pixel format
        /// </summary>
        PixelFormat Format { get; }
    }
}
