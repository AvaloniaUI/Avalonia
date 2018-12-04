using System;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging
{
    public class ForeignBitmap : Bitmap
    {
        public ForeignBitmap(IForeignBitmapImpl impl) : base(impl)
        {
            
        }
        
        /// <summary>
        /// Locks the bitmap, so underlying surface can be safely updated from the current thread
        /// </summary>
        /// <returns>IDisposable object for unlocking the bitmap</returns>
        public IDisposable Lock() => ((IForeignBitmapImpl)PlatformImpl.Item).Lock();
    }
}
