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
    public class ImmutableBitmap : IDrawableBitmapImpl
    {
        private readonly SKImage _image;

        /// <summary>
        /// Create immutable bitmap from given stream.
        /// </summary>
        /// <param name="stream">Stream containing encoded data.</param>
        public ImmutableBitmap(Stream stream)
        {
            using (var skiaStream = new SKManagedStream(stream))
            {
                _image = SKImage.FromEncodedData(SKData.Create(skiaStream));

                if (_image == null)
                {
                    throw new ArgumentException("Unable to load bitmap from provided data");
                }

                PixelWidth = _image.Width;
                PixelHeight = _image.Height;
            }
        }

        /// <summary>
        /// Create immutable bitmap from given pixel data copy.
        /// </summary>
        /// <param name="width">Width of data pixels.</param>
        /// <param name="height">Height of data pixels.</param>
        /// <param name="stride">Stride of data pixels.</param>
        /// <param name="format">Format of data pixels.</param>
        /// <param name="data">Data pixels.</param>
        public ImmutableBitmap(int width, int height, int stride, PixelFormat format, IntPtr data)
        {
            var imageInfo = new SKImageInfo(width, height, format.ToSkColorType(), SKAlphaType.Premul);

            _image = SKImage.FromPixelCopy(imageInfo, data, stride);

            if (_image == null)
            {
                throw new ArgumentException("Unable to create bitmap from provided data");
            }

            PixelWidth = width;
            PixelHeight = height;
        }

        /// <inheritdoc />
        public int PixelWidth { get; }

        /// <inheritdoc />
        public int PixelHeight { get; }

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
            context.Canvas.DrawImage(_image, sourceRect, destRect, paint);
        }
    }
}