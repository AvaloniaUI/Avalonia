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
    /// Holds a writable bitmap image.
    /// </summary>
    public class WritableBitmap : Bitmap
    {
        public WritableBitmap(int width, int height, PixelFormat? format = null) 
            : base(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateWritableBitmap(width, height, format))
        {
        }
        
        public ILockedFramebuffer Lock() => ((IWritableBitmapImpl) PlatformImpl.Item).Lock();
    }
}
