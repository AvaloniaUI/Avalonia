using System;
using System.IO;
using System.Runtime.CompilerServices;
using Avalonia.Platform;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Holds a writeable bitmap image.
    /// </summary>
    public class WriteableBitmap : Bitmap
    {
        // Holds a buffer with pixel format that requires transcoding
        private readonly BitmapMemory? _pixelFormatMemory = null;
        
        /// <summary>
        /// Initializes a new instance of the <see cref="WriteableBitmap"/> class.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format (optional).</param>
        /// <param name="alphaFormat">The alpha format (optional).</param>
        /// <returns>An instance of the <see cref="WriteableBitmap"/> class.</returns>
        public WriteableBitmap(PixelSize size, Vector dpi, PixelFormat? format = null, AlphaFormat? alphaFormat = null) 
            : this(CreatePlatformImpl(size, dpi, format, alphaFormat))
        {
        }

        private WriteableBitmap((IBitmapImpl impl, BitmapMemory? mem) bitmapWithMem) : this(bitmapWithMem.impl,
            bitmapWithMem.mem)
        {
            
        }
        
        private WriteableBitmap(IBitmapImpl impl, BitmapMemory? pixelFormatMemory = null) : base(impl)
        {
            _pixelFormatMemory = pixelFormatMemory;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WriteableBitmap"/> class with existing pixel data
        /// The data is copied to the bitmap
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="alphaFormat">The alpha format.</param>
        /// <param name="data">The pointer to the source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        public unsafe WriteableBitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
            : this(size, dpi, format, alphaFormat)
        {
            var minStride = (format.BitsPerPixel * size.Width + 7) / 8;
            if (minStride > stride)
                throw new ArgumentOutOfRangeException(nameof(stride));

            using (var locked = Lock())
            {
                for (var y = 0; y < size.Height; y++)
                    Unsafe.CopyBlock((locked.Address + locked.RowBytes * y).ToPointer(),
                        (data + y * stride).ToPointer(), (uint)minStride);
            }
        }

        public override PixelFormat? Format => _pixelFormatMemory?.Format ?? base.Format;
        
        public ILockedFramebuffer Lock()
        {
            if (_pixelFormatMemory == null)
                return ((IWriteableBitmapImpl)PlatformImpl.Item).Lock();
            
            return new LockedFramebuffer(_pixelFormatMemory.Address, _pixelFormatMemory.Size,
                _pixelFormatMemory.RowBytes,
                Dpi, _pixelFormatMemory.Format, () =>
                {
                    using var inner = ((IWriteableBitmapImpl)PlatformImpl.Item).Lock();
                    _pixelFormatMemory.CopyToRgba(Platform.AlphaFormat.Unpremul, inner.Address, inner.RowBytes);
                });
        }

        public override void CopyPixels(PixelRect sourceRect, IntPtr buffer, int bufferSize, int stride)
        {
            using (var fb = Lock())
                CopyPixelsCore(sourceRect, buffer, bufferSize, stride, fb);
        }

        public static WriteableBitmap Decode(Stream stream)
        {
            var ri = GetFactory();

            return new WriteableBitmap(ri.LoadWriteableBitmap(stream));
        }
        
        /// <summary>
        /// Loads a WriteableBitmap from a stream and decodes at the desired width. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="WriteableBitmap"/> class.</returns>
        public new static WriteableBitmap DecodeToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            var ri = GetFactory();

            return new WriteableBitmap(ri.LoadWriteableBitmapToWidth(stream, width, interpolationMode));
        }

        /// <summary>
        /// Loads a Bitmap from a stream and decodes at the desired height. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="WriteableBitmap"/> class.</returns>
        public new static WriteableBitmap DecodeToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            var ri = GetFactory();

            return new WriteableBitmap(ri.LoadWriteableBitmapToHeight(stream, height, interpolationMode));
        }

        private static (IBitmapImpl, BitmapMemory?) CreatePlatformImpl(PixelSize size, in Vector dpi, PixelFormat? format, AlphaFormat? alphaFormat)
        {
            if (size.Width <= 0 || size.Height <= 0)
                throw new ArgumentException("Size should be >= (1,1)", nameof(size));
            
            var ri = GetFactory();

            PixelFormat finalFormat = format ?? ri.DefaultPixelFormat;
            AlphaFormat finalAlphaFormat = alphaFormat ?? ri.DefaultAlphaFormat;

            if (ri.IsSupportedBitmapPixelFormat(finalFormat))
                return (ri.CreateWriteableBitmap(size, dpi, finalFormat, finalAlphaFormat), null);

            if (!PixelFormatReader.SupportsFormat(finalFormat))
                throw new NotSupportedException($"Pixel format {finalFormat} is not supported");

            finalAlphaFormat = finalFormat.HasAlpha ? finalAlphaFormat : Platform.AlphaFormat.Opaque;

            var impl = ri.CreateWriteableBitmap(size, dpi, PixelFormat.Rgba8888, finalAlphaFormat);
            return (impl, new BitmapMemory(finalFormat, finalAlphaFormat, size));
        }

        private static IPlatformRenderInterface GetFactory()
        {
            return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        }
    }
}
