using Avalonia.Platform;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Holds a writeable bitmap image.
    /// </summary>
    public class WriteableBitmap : Bitmap
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteableBitmap"/> class.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format (optional).</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        public WriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null) 
            : base(AvaloniaLocator.Current.GetService<IPlatformRenderInterface>().CreateWriteableBitmap(size, dpi, format))
        {
        }
        
        public ILockedFramebuffer Lock() => ((IWriteableBitmapImpl) PlatformImpl.Item).Lock();
    }
}
