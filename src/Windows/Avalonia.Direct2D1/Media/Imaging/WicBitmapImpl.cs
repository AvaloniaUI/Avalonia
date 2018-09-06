// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using Avalonia.Win32.Interop;
using SharpDX.WIC;
using APixelFormat = Avalonia.Platform.PixelFormat;
using D2DBitmap = SharpDX.Direct2D1.Bitmap;

namespace Avalonia.Direct2D1.Media
{
    /// <summary>
    /// A WIC implementation of a <see cref="Avalonia.Media.Imaging.Bitmap"/>.
    /// </summary>
    public class WicBitmapImpl : BitmapImpl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="WicBitmapImpl"/> class.
        /// </summary>
        /// <param name="fileName">The filename of the bitmap to load.</param>
        public WicBitmapImpl(string fileName)
        {
            using (BitmapDecoder decoder = new BitmapDecoder(Direct2D1Platform.ImagingFactory, fileName, DecodeOptions.CacheOnDemand))
            {
                WicImpl = new Bitmap(Direct2D1Platform.ImagingFactory, decoder.GetFrame(0), BitmapCreateCacheOption.CacheOnDemand);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WicBitmapImpl"/> class.
        /// </summary>
        /// <param name="stream">The stream to read the bitmap from.</param>
        public WicBitmapImpl(Stream stream)
        {
            using (BitmapDecoder decoder = new BitmapDecoder(Direct2D1Platform.ImagingFactory, stream, DecodeOptions.CacheOnLoad))
            {
                WicImpl = new Bitmap(Direct2D1Platform.ImagingFactory, decoder.GetFrame(0), BitmapCreateCacheOption.CacheOnLoad);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="WicBitmapImpl"/> class.
        /// </summary>
        /// <param name="width">The width of the bitmap.</param>
        /// <param name="height">The height of the bitmap.</param>
        /// <param name="pixelFormat">Pixel format</param>
        public WicBitmapImpl(int width, int height, APixelFormat? pixelFormat = null)
        {
            if (!pixelFormat.HasValue)
            {
                pixelFormat = APixelFormat.Bgra8888;
            }

            PixelFormat = pixelFormat;
            WicImpl = new Bitmap(
                Direct2D1Platform.ImagingFactory,
                width,
                height,
                pixelFormat.Value.ToWic(),
                BitmapCreateCacheOption.CacheOnLoad);
        }

        public WicBitmapImpl(APixelFormat format, IntPtr data, int width, int height, int stride)
        {
            WicImpl = new Bitmap(Direct2D1Platform.ImagingFactory, width, height, format.ToWic(), BitmapCreateCacheOption.CacheOnDemand);
            PixelFormat = format;
            using (var l = WicImpl.Lock(BitmapLockFlags.Write))
            {
                for (var row = 0; row < height; row++)
                {
                    UnmanagedMethods.CopyMemory(
                        (l.Data.DataPointer + row * l.Stride),
                        (data + row * stride),
                        (UIntPtr) l.Data.Pitch);
                }
            }
        }

        protected APixelFormat? PixelFormat { get; }

        /// <summary>
        /// Gets the width of the bitmap, in pixels.
        /// </summary>
        public override int PixelWidth => WicImpl.Size.Width;

        /// <summary>
        /// Gets the height of the bitmap, in pixels.
        /// </summary>
        public override int PixelHeight => WicImpl.Size.Height;

        public override void Dispose()
        {
            WicImpl.Dispose();
        }

        /// <summary>
        /// Gets the WIC implementation of the bitmap.
        /// </summary>
        public Bitmap WicImpl { get; }

        /// <summary>
        /// Gets a Direct2D bitmap to use on the specified render target.
        /// </summary>
        /// <param name="renderTarget">The render target.</param>
        /// <returns>The Direct2D bitmap.</returns>
        public override OptionalDispose<D2DBitmap> GetDirect2DBitmap(SharpDX.Direct2D1.RenderTarget renderTarget)
        {
            FormatConverter converter = new FormatConverter(Direct2D1Platform.ImagingFactory);
            converter.Initialize(WicImpl, SharpDX.WIC.PixelFormat.Format32bppPBGRA);
            return new OptionalDispose<D2DBitmap>(D2DBitmap.FromWicBitmap(renderTarget, converter), true);
        }

        public override void Save(Stream stream)
        {
            using (var encoder = new PngBitmapEncoder(Direct2D1Platform.ImagingFactory, stream))
            using (var frame = new BitmapFrameEncode(encoder))
            {
                frame.Initialize();
                frame.WriteSource(WicImpl);
                frame.Commit();
                encoder.Commit();
            }
        }
    }
}
