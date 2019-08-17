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
    /// Immutable Skia bitmap.
    /// </summary>
    internal class ImmutableBitmap : IDrawableBitmapImpl
    {
        private GRContext _grContext;
        private SKImage _image;
        
        /// <summary>
        /// Create immutable bitmap from given stream.
        /// </summary>
        /// <param name="grContext">A valid GRContext if this bitmap should be created on the GPU; null otherwise.</param>
        /// <param name="stream">Stream containing encoded data.</param>
        public ImmutableBitmap(GRContext grContext, Stream stream)
        {
            _grContext = grContext;

            using (var skiaStream = new SKManagedStream(stream))
            {
                using (var data = SKData.Create(skiaStream))
                    _image = SKImage.FromEncodedData(data);

                if (_image == null)
                {
                    throw new ArgumentException("Unable to load bitmap from provided data");
                }

                PixelSize = new PixelSize(_image.Width, _image.Height);

                // TODO: Skia doesn't have an API for DPI.
                Dpi = new Vector(96, 96);
            }
        }

        /// <summary>
        /// Create immutable bitmap from given pixel data copy.
        /// </summary>
        /// <param name="grContext">A valid GRContext if this bitmap should be created on the GPU; null otherwise.</param>
        /// <param name="size">Size of the bitmap.</param>
        /// <param name="dpi">DPI of the bitmap.</param>
        /// <param name="stride">Stride of data pixels.</param>
        /// <param name="format">Format of data pixels.</param>
        /// <param name="data">Data pixels.</param>
        public ImmutableBitmap(
            GRContext grContext, PixelSize size, Vector dpi, int stride, PixelFormat format, IntPtr data)
        {
            _grContext = grContext;

            _image = SKImage.FromPixelCopy(
                new SKImageInfo(size.Width, size.Height, format.ToSkColorType(), SKAlphaType.Premul),
                data,
                stride);
            
            if (_image == null)
            {
                throw new ArgumentException("Unable to create bitmap from provided data");
            }

            PixelSize = size;
            Dpi = dpi;
        }

        public Vector Dpi { get; }
        public PixelSize PixelSize { get; }

        public int Version { get; } = 1;

        /// <inheritdoc />
        public void Dispose()
        {
            _image.Dispose();
        }

        /// <inheritdoc />
        public void Save(string fileName)
        {
            ImageSavingHelper.SaveImage(_image, fileName);
        }

        /// <inheritdoc />
        public void Save(Stream stream)
        {
            ImageSavingHelper.SaveImage(_image, stream);
        }

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            if (_grContext != null)
            {
                var imageInfo = new SKImageInfo(
                    _image.Width, _image.Height, SKColorType.Bgra8888, _image.AlphaType, _image.ColorSpace);

                using (var surface = SKSurface.Create(_grContext, false, imageInfo, 1, GRSurfaceOrigin.TopLeft))
                {
                    if (surface != null)
                    {
                        surface.Canvas.DrawImage(_image, 0, 0);
                        surface.Canvas.Flush();
                        _image.Dispose();
                        _image = surface.Snapshot();
                    }
                }

                _grContext = null;
            }

            context.Canvas.DrawImage(_image, sourceRect, destRect, paint);
        }
    }
}
