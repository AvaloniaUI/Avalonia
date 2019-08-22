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
        private readonly SKImage _image;
        private readonly SKBitmap _bitmap;
        private readonly SKPixmap _pixmap;
        private readonly IUnmanagedBlob _blob;

        private SKSurface _surface;

        internal readonly object _lock = new object();

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
                var nfo = new SKImageInfo(size.Width, size.Height, colorType, SKAlphaType.Premul);

                _blob = runtimePlatform.AllocBlob(nfo.BytesSize);

                _pixmap = new SKPixmap(nfo, _blob.Address, nfo.RowBytes);

                _bitmap = new SKBitmap();

                _bitmap.InstallPixels(_pixmap);

                _bitmap.SetImmutable();

                _image = SKImage.FromBitmap(_bitmap);
            }
            else
            {
                _bitmap = new SKBitmap(size.Width, size.Height, colorType, SKAlphaType.Premul);

                _pixmap = _bitmap.PeekPixels();

                _image = SKImage.FromBitmap(_bitmap);
            }

            _pixmap.Erase(SKColor.Empty);
        }

        public Vector Dpi { get; }

        /// <inheritdoc />
        public PixelSize PixelSize { get; }

        public int Version { get; internal set; } = 1;

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            lock (_lock)
                context.Canvas.DrawImage(_image, sourceRect, destRect, paint);
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _surface?.Dispose();
            _image.Dispose();
            _bitmap.Dispose();
            _pixmap.Dispose();
            _blob.Dispose();
        }

        public IDrawingContextImpl CreateDrawingContext(IVisualBrushRenderer visualBrushRenderer)
        {
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

            return new DrawingContextImpl(createInfo, Disposable.Create(() => _bitmap.NotifyPixelsChanged()));
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
                return SKImage.FromPixelCopy(_pixmap);
        }
    }

    /// <summary>
    /// Framebuffer for Pixmap.
    /// </summary>
    public class BitmapFramebuffer : ILockedFramebuffer
    {
        private WriteableBitmapImpl _parent;
        private SKBitmap _bitmap;

        /// <summary>
        /// Create framebuffer from given bitmap.
        /// </summary>
        /// <param name="parent">Parent bitmap impl.</param>
        /// <param name="bitmap">Bitmap</param>
        internal BitmapFramebuffer(WriteableBitmapImpl parent, SKBitmap bitmap)
        {
            _parent = parent;
            _bitmap = bitmap;
            var pixmap = bitmap.PeekPixels();
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
