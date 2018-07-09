// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia based writeable bitmap.
    /// </summary>
    public class WriteableBitmapImpl : IWriteableBitmapImpl, IDrawableBitmapImpl
    {
        private static readonly SKBitmapReleaseDelegate s_releaseDelegate = ReleaseProc;
        private readonly SKBitmap _bitmap;

        /// <summary>
        /// Create new writeable bitmap.
        /// </summary>
        /// <param name="width">Width.</param>
        /// <param name="height">Height.</param>
        /// <param name="format">Format.</param>
        public WriteableBitmapImpl(int width, int height, PixelFormat? format = null)
        {
            PixelHeight = height;
            PixelWidth = width;

            var colorType = PixelFormatHelper.ResolveColorType(format);
            
            var runtimePlatform = AvaloniaLocator.Current?.GetService<IRuntimePlatform>();
            
            if (runtimePlatform != null)
            {
                _bitmap = new SKBitmap();

                var nfo = new SKImageInfo(width, height, colorType, SKAlphaType.Premul);
                var blob = runtimePlatform.AllocBlob(nfo.BytesSize);

                _bitmap.InstallPixels(nfo, blob.Address, nfo.RowBytes, null, s_releaseDelegate, blob);
            }
            else
            {
                _bitmap = new SKBitmap(width, height, colorType, SKAlphaType.Premul);
            }

            _bitmap.Erase(SKColor.Empty);
        }

        /// <inheritdoc />
        public int PixelWidth { get; }

        /// <inheritdoc />
        public int PixelHeight { get; }

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            context.Canvas.DrawBitmap(_bitmap, sourceRect, destRect, paint);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _bitmap.Dispose();
        }

        /// <inheritdoc />
        public void Save(Stream stream)
        {
            using (var image = GetSnapshot())
            {
                ImageSavingHelper.SaveImage(image, stream);
            }
        }

        /// <inheritdoc />
        public void Save(string fileName)
        {
            using (var image = GetSnapshot())
            {
                ImageSavingHelper.SaveImage(image, fileName);
            }
        }

        /// <inheritdoc />
        public ILockedFramebuffer Lock() => new BitmapFramebuffer(_bitmap);

        /// <summary>
        /// Get snapshot as image.
        /// </summary>
        /// <returns>Image snapshot.</returns>
        public SKImage GetSnapshot()
        {
            return SKImage.FromPixels(_bitmap.Info, _bitmap.GetPixels(), _bitmap.RowBytes);
        }

        /// <summary>
        /// Release given unmanaged blob.
        /// </summary>
        /// <param name="address">Blob address.</param>
        /// <param name="ctx">Blob.</param>
        private static void ReleaseProc(IntPtr address, object ctx)
        {
            ((IUnmanagedBlob)ctx).Dispose();
        }

        /// <summary>
        /// Framebuffer for bitmap.
        /// </summary>
        private class BitmapFramebuffer : ILockedFramebuffer
        {
            private SKBitmap _bitmap;

            /// <summary>
            /// Create framebuffer from given bitmap.
            /// </summary>
            /// <param name="bitmap">Bitmap.</param>
            public BitmapFramebuffer(SKBitmap bitmap)
            {
                _bitmap = bitmap;
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _bitmap = null;
            }
            
            /// <inheritdoc />
            public IntPtr Address => _bitmap.GetPixels();

            /// <inheritdoc />
            public int Width => _bitmap.Width;

            /// <inheritdoc />
            public int Height => _bitmap.Height;

            /// <inheritdoc />
            public int RowBytes => _bitmap.RowBytes;

            /// <inheritdoc />
            public Vector Dpi { get; } = SkiaPlatform.DefaultDpi;

            /// <inheritdoc />
            public PixelFormat Format => _bitmap.ColorType.ToPixelFormat();
        }
    }
}