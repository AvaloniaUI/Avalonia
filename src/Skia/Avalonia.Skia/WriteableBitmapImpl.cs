// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Skia based writeable bitmap.
    /// </summary>
    internal class WriteableBitmapImpl : IWriteableBitmapImpl, IDrawableBitmapImpl
    {
        private static readonly SKBitmapReleaseDelegate s_releaseDelegate = ReleaseProc;
        private readonly object _lock = new object();
        private readonly SKBitmap _bitmap;
        private SKSurface _surface;
        private GRContext _grContext;
        private int _lastVersion;

        /// <summary>
        /// Create new writeable bitmap.
        /// </summary>
        /// <param name="grContext">A valid GRContext if this bitmap should be created on the GPU; null otherwise.</param>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format.</param>
        public WriteableBitmapImpl(GRContext grContext, PixelSize size, Vector dpi, PixelFormat? format = null)
        {
            _grContext = grContext;
            PixelSize = size;
            Dpi = dpi;
            
            var colorType = PixelFormatHelper.ResolveColorType(format);
            
            var runtimePlatform = AvaloniaLocator.Current?.GetService<IRuntimePlatform>();
            
            if (runtimePlatform != null)
            {
                _bitmap = new SKBitmap();

                var nfo = new SKImageInfo(size.Width, size.Height, colorType, SKAlphaType.Premul);
                var blob = runtimePlatform.AllocBlob(nfo.BytesSize);

                _bitmap.InstallPixels(nfo, blob.Address, nfo.RowBytes, s_releaseDelegate, blob);
            }
            else
            {
                _bitmap = new SKBitmap(size.Width, size.Height, colorType, SKAlphaType.Premul);
            }

            _bitmap.Erase(SKColor.Empty);
        }

        public Vector Dpi { get; }

        /// <inheritdoc />
        public PixelSize PixelSize { get; }

        public int Version { get; private set; } = 1;

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
            {
                if (_grContext != null)
                {
                    var imageInfo = new SKImageInfo(
                        _bitmap.Width, _bitmap.Height, SKColorType.Bgra8888, _bitmap.AlphaType, _bitmap.ColorSpace);

                    _surface = SKSurface.Create(_grContext, false, imageInfo, 1, GRSurfaceOrigin.TopLeft);
                    _grContext = null;
                }

                if (_surface != null)
                {
                    if (Version > _lastVersion)
                    {
                        _surface.Canvas.Clear();
                        _surface.Canvas.DrawBitmap(
                            _bitmap, sourceRect, SKRect.Create(0, 0, sourceRect.Width, sourceRect.Height));
                        _lastVersion = Version;
                    }
                    
                    _surface.Draw(context.Canvas, destRect.Left, destRect.Top, paint);
                }
                else
                {
                    context.Canvas.DrawBitmap(_bitmap, sourceRect, destRect, paint);
                }
            }
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _surface?.Dispose();
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
        public ILockedFramebuffer Lock() => new BitmapFramebuffer(this, _bitmap);

        /// <summary>
        /// Get snapshot as image.
        /// </summary>
        /// <returns>Image snapshot.</returns>
        public SKImage GetSnapshot()
        {
            lock (_lock)
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
            private WriteableBitmapImpl _parent;
            private SKBitmap _bitmap;

            /// <summary>
            /// Create framebuffer from given bitmap.
            /// </summary>
            /// <param name="parent">Parent bitmap impl.</param>
            /// <param name="bitmap">Bitmap.</param>
            public BitmapFramebuffer(WriteableBitmapImpl parent, SKBitmap bitmap)
            {
                _parent = parent;
                _bitmap = bitmap;
                Address = _bitmap.GetPixels();
                Size = new PixelSize(_bitmap.Width, _bitmap.Height);
                RowBytes = _bitmap.RowBytes;
                Format = _bitmap.ColorType.ToPixelFormat();
                Monitor.Enter(parent._lock);
            }

            /// <inheritdoc />
            public void Dispose()
            {
                _bitmap.NotifyPixelsChanged();
                _parent.Version++;
                Monitor.Exit(_parent._lock);
                _bitmap = null;
                _parent = null;
            }
            
            /// <inheritdoc />
            public IntPtr Address { get; }

            /// <inheritdoc />
            public PixelSize Size { get; }

            /// <inheritdoc />
            public int RowBytes { get; }

            /// <inheritdoc />
            public Vector Dpi { get; } = SkiaPlatform.DefaultDpi;

            /// <inheritdoc />
            public PixelFormat Format { get; }
        }
    }
}
