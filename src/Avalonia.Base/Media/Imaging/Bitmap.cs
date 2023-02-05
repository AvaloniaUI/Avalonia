using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Avalonia.Platform;
using Avalonia.Utilities;

namespace Avalonia.Media.Imaging
{
    /// <summary>
    /// Holds a bitmap image.
    /// </summary>
    public class Bitmap : IBitmap
    {
        private bool _isTranscoded;
        /// <summary>
        /// Loads a Bitmap from a stream and decodes at the desired width. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="width">The desired width of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="Bitmap"/> class.</returns>
        public static Bitmap DecodeToWidth(Stream stream, int width, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new Bitmap(GetFactory().LoadBitmapToWidth(stream, width, interpolationMode));
        }

        /// <summary>
        /// Loads a Bitmap from a stream and decodes at the desired height. Aspect ratio is maintained.
        /// This is more efficient than loading and then resizing.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from. This can be any supported image format.</param>
        /// <param name="height">The desired height of the resulting bitmap.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="Bitmap"/> class.</returns>
        public static Bitmap DecodeToHeight(Stream stream, int height, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new Bitmap(GetFactory().LoadBitmapToHeight(stream, height, interpolationMode));
        }

        /// <summary>
        /// Creates a Bitmap scaled to a specified size from the current bitmap.
        /// </summary>        
        /// <param name="destinationSize">The destination size.</param>
        /// <param name="interpolationMode">The <see cref="BitmapInterpolationMode"/> to use should any scaling be required.</param>
        /// <returns>An instance of the <see cref="Bitmap"/> class.</returns>
        public Bitmap CreateScaledBitmap(PixelSize destinationSize, BitmapInterpolationMode interpolationMode = BitmapInterpolationMode.HighQuality)
        {
            return new Bitmap(GetFactory().ResizeBitmap(PlatformImpl.Item, destinationSize, interpolationMode));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap.</param>
        public Bitmap(string fileName)
        {
            PlatformImpl = RefCountable.Create(GetFactory().LoadBitmap(fileName));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>
        public Bitmap(Stream stream)
        {
            PlatformImpl = RefCountable.Create(GetFactory().LoadBitmap(stream));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific bitmap implementation.</param>
        public Bitmap(IRef<IBitmapImpl> impl)
        {
            PlatformImpl = impl.Clone();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="impl">A platform-specific bitmap implementation. Bitmap class takes the ownership.</param>
        protected Bitmap(IBitmapImpl impl)
        {
            PlatformImpl = RefCountable.Create(impl);
        }

        /// <inheritdoc/>
        public virtual void Dispose()
        {
            PlatformImpl.Dispose();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Bitmap"/> class.
        /// </summary>
        /// <param name="format">The pixel format.</param>
        /// <param name="alphaFormat">The alpha format.</param>
        /// <param name="data">The pointer to the source bytes.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="stride">The number of bytes per row.</param>
        public Bitmap(PixelFormat format, AlphaFormat alphaFormat, IntPtr data, PixelSize size, Vector dpi, int stride)
        {
            var factory = GetFactory();
            if (factory.IsSupportedBitmapPixelFormat(format))
                PlatformImpl = RefCountable.Create(factory.LoadBitmap(format, alphaFormat, data, size, dpi, stride));
            else
            {
                var transcoded = Marshal.AllocHGlobal(size.Width * size.Height * 4);
                var transcodedStride = size.Width * 4;
                try
                {
                    PixelFormatReader.Transcode(transcoded, data, size, stride, transcodedStride, format);
                    var transcodedAlphaFormat = format.HasAlpha ? alphaFormat : AlphaFormat.Opaque;
                    
                    PlatformImpl = RefCountable.Create(factory.LoadBitmap(PixelFormat.Rgba8888, transcodedAlphaFormat,
                        transcoded, size, dpi, transcodedStride));
                }
                finally
                {
                    Marshal.FreeHGlobal(transcoded);
                }

                _isTranscoded = true;
            }
        }

        /// <inheritdoc/>
        public Vector Dpi => PlatformImpl.Item.Dpi;

        /// <inheritdoc/>
        public Size Size => PlatformImpl.Item.PixelSize.ToSizeWithDpi(Dpi);

        /// <inheritdoc/>
        public PixelSize PixelSize => PlatformImpl.Item.PixelSize;

        /// <summary>
        /// Gets the platform-specific bitmap implementation.
        /// </summary>
        public IRef<IBitmapImpl> PlatformImpl { get; }

        /// <summary>
        /// Saves the bitmap to a file.
        /// </summary>
        /// <param name="fileName">The filename.</param>
        /// <param name="quality">
        /// The optional quality for compression. 
        /// The quality value is interpreted from 0 - 100. If quality is null the default quality 
        /// setting is applied.
        /// </param>
        public void Save(string fileName, int? quality = null)
        {
            PlatformImpl.Item.Save(fileName, quality);
        }

        /// <summary>
        /// Saves the bitmap to a stream.
        /// </summary>
        /// <param name="stream">The stream.</param>
        /// <param name="quality">
        /// The optional quality for compression. 
        /// The quality value is interpreted from 0 - 100. If quality is null the default quality 
        /// setting is applied.
        /// </param>
        public void Save(Stream stream, int? quality = null)
        {
            PlatformImpl.Item.Save(stream, quality);
        }

        public virtual PixelFormat? Format => (PlatformImpl.Item as IReadableBitmapImpl)?.Format;

        protected internal unsafe void CopyPixelsCore(PixelRect sourceRect, IntPtr buffer, int bufferSize, int stride,
            ILockedFramebuffer fb)
        {
            if ((sourceRect.Width <= 0 || sourceRect.Height <= 0) && (sourceRect.X != 0 || sourceRect.Y != 0))
                throw new ArgumentOutOfRangeException(nameof(sourceRect));

            if (sourceRect.X < 0 || sourceRect.Y < 0)
                throw new ArgumentOutOfRangeException(nameof(sourceRect));
            
            if (sourceRect.Width <= 0)
                sourceRect = sourceRect.WithWidth(PixelSize.Width);
            if (sourceRect.Height <= 0)
                sourceRect = sourceRect.WithHeight(PixelSize.Height);

            if (sourceRect.Right > PixelSize.Width || sourceRect.Bottom > PixelSize.Height)
                throw new ArgumentOutOfRangeException(nameof(sourceRect));
            
            int minStride = checked(((sourceRect.Width * fb.Format.BitsPerPixel) + 7) / 8);
            if (stride < minStride)
                throw new ArgumentOutOfRangeException(nameof(stride));

            var minBufferSize = stride * sourceRect.Height;
            if (minBufferSize > bufferSize)
                throw new ArgumentOutOfRangeException(nameof(bufferSize));

            for (var y = 0; y < sourceRect.Height; y++)
            {
                var srcAddress = fb.Address + fb.RowBytes * y;
                var dstAddress = buffer + stride * y;
                Unsafe.CopyBlock(dstAddress.ToPointer(), srcAddress.ToPointer(), (uint)minStride);
            }
        }
        
        public virtual void CopyPixels(PixelRect sourceRect, IntPtr buffer, int bufferSize, int stride)
        {
            if (
                Format == null
                || PlatformImpl.Item is not IReadableBitmapImpl readable
                || Format != readable.Format
            )
                throw new NotSupportedException("CopyPixels is not supported for this bitmap type");
            
            if (_isTranscoded)
                throw new NotSupportedException("CopyPixels is not supported for transcoded bitmaps");
            
            using (var fb = readable.Lock())
                CopyPixelsCore(sourceRect, buffer, bufferSize, stride, fb);
        }

        /// <inheritdoc/>
        void IImage.Draw(
            DrawingContext context,
            Rect sourceRect,
            Rect destRect,
            BitmapInterpolationMode bitmapInterpolationMode)
        {
            context.PlatformImpl.DrawBitmap(
                PlatformImpl,
                1,
                sourceRect,
                destRect,
                bitmapInterpolationMode);
        }

        private static IPlatformRenderInterface GetFactory()
        {
            return AvaloniaLocator.Current.GetRequiredService<IPlatformRenderInterface>();
        }
    }
}
