using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Avalonia.Platform
{
    /// <summary>
    /// Defines the platform-specific interface for a <see cref="Avalonia.Media.Imaging.WritableBitmap"/>.
    /// </summary>
    public interface IWritableBitmapImpl : IBitmapImpl
    {
        ILockedFramebuffer Lock();
    }
}
