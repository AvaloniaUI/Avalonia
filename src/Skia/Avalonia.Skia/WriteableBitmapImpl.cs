// Copyright (c) The Avalonia Project. All rights reserved.
// Licensed under the MIT license. See licence.md file in the project root for full license information.

using System;
using System.IO;
using System.Reactive.Disposables;
using System.Threading;
using Avalonia.Platform;
using Avalonia.Rendering;
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
        private readonly SKBitmap _bitmap;
        private readonly SKPixmap _pixmap;
        private readonly object _lock = new object();

        private SKSurface _surface;

        /// <summary>
        /// Create new writeable bitmap.
        /// </summary>
        /// <param name="size">The size of the bitmap in device pixels.</param>
        /// <param name="dpi">The DPI of the bitmap.</param>
        /// <param name="format">The pixel format.</param>
        public WriteableBitmapImpl(PixelSize size, Vector dpi, PixelFormat? format = null)
        {
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

            _pixmap = _bitmap.PeekPixels();
        }

        public Vector Dpi { get; }

        /// <inheritdoc />
        public PixelSize PixelSize { get; }

        public int Version { get; private set; } = 1;

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
                context.Canvas.DrawBitmap(_bitmap, sourceRect, destRect, paint);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _bitmap.Dispose();
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
            Monitor.Enter(_lock);

            if (_surface == null)
            {
                _surface = SKSurface.Create(_pixmap);
            }

            var createInfo = new DrawingContextImpl.CreateInfo
            {
                Canvas = _surface.Canvas,
                Dpi = Dpi,
                VisualBrushRenderer = visualBrushRenderer
            };

            return new DrawingContextImpl(createInfo, Disposable.Create(() =>
            {
                _bitmap.NotifyPixelsChanged();
                Monitor.Exit(_lock);
            }));
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
        public ILockedFramebuffer Lock() => new BitmapFramebuffer(this, _bitmap, _pixmap);

        /// <summary>
        /// Get snapshot as image.
        /// </summary>
        /// <returns>Image snapshot.</returns>
        public SKImage GetSnapshot()
        {
            lock (_lock)
                return SKImage.FromPixelCopy(_pixmap);
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
            /// <param name="bitmap">Bitmap</param>
            /// <param name="pixmap">Pixmap</param>
            internal BitmapFramebuffer(WriteableBitmapImpl parent, SKBitmap bitmap, SKPixmap pixmap)
            {
                _parent = parent;
                _bitmap = bitmap;
                Address = pixmap.GetPixels();
                Size = new PixelSize(pixmap.Width, pixmap.Height);
                RowBytes = pixmap.RowBytes;
                Dpi = SkiaPlatform.DefaultDpi;
                Format = pixmap.ColorType.ToPixelFormat();
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
            public Vector Dpi { get; }

            /// <inheritdoc />
            public PixelFormat Format { get; }
        }
    }
}
