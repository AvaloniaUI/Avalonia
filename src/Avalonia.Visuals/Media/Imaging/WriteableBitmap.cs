using System;
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
        [Obsolete("Use overload taking an AlphaFormat.")]
        public WriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null)
            : base(CreatePlatformImpl(size, dpi, format, null))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteableBitmap"/> class.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format (optional).</param>
        /// <param name="alphaFormat">The alpha format (optional).</param>
        /// <returns>An <see cref="IWriteableBitmapImpl"/>.</returns>
        public WriteableBitmap(PixelSize size, Vector dpi, PixelFormat format, AlphaFormat alphaFormat) 
            : base(CreatePlatformImpl(size, dpi, format, alphaFormat))
        {
        }

        public ILockedFramebuffer Lock() => ((IWriteableBitmapImpl) PlatformImpl.Item).Lock();

        private static IBitmapImpl CreatePlatformImpl(PixelSize size, in Vector dpi, PixelFormat? format, AlphaFormat? alphaFormat)
        {
            var ri = AvaloniaLocator.Current.GetService<IPlatformRenderInterface>();

            PixelFormat finalFormat = format ?? ri.DefaultPixelFormat;
            AlphaFormat finalAlphaFormat = alphaFormat ?? ri.DefaultAlphaFormat;

            return ri.CreateWriteableBitmap(size, dpi, finalFormat, finalAlphaFormat);
        }
    }
}
