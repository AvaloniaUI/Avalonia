using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Controls.Platform.Surfaces
{
    public interface ILockedFramebuffer : IDisposable
    {
        IntPtr Address { get; }
        int Width { get; }
        int Height { get; }
        int RowBytes { get; }
        Size Dpi { get; }
        PixelFormat Format { get; }
    }
}
