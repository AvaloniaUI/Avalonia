using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Holds a writeable bitmap image.
    /// </summary>
    public class WriteableBitmap : Bitmap
    {
        public WriteableBitmap(int width, int height, PixelFormat? format = null) 
            : base(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateWriteableBitmap(width, height, format))
        {
        }
        
        public ILockedFramebuffer Lock() => ((IWriteableBitmapImpl) PlatformImpl.Item).Lock();
    }
}
