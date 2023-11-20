using System;
using System.IO;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia.Helpers;
using SkiaSharp;

namespace Avalonia.Skia
{
    /// <summary>
    /// Immutable Skia bitmap.
    /// </summary>
    internal class ImmutableBitmap : IDrawableBitmapImpl, IReadableBitmapWithAlphaImpl
    {
        private readonly SKImage _image;
        private readonly SKBitmap? _bitmap;

        /// <summary>
        /// Create immutable bitmap from given stream.
        /// </summary>
        /// <param name="stream">Stream containing encoded data.</param>
        public ImmutableBitmap(Stream stream)
        {
            using (var skiaStream = new SKManagedStream(stream))
            {
                using (var data = SKData.Create(skiaStream))
                    _bitmap = SKBitmap.Decode(data);
                
                if (_bitmap == null)
                    throw new ArgumentException("Unable to load bitmap from provided data");

                _bitmap.SetImmutable();
                _image = SKImage.FromBitmap(_bitmap);

                PixelSize = new PixelSize(_image.Width, _image.Height);

                // TODO: Skia doesn't have an API for DPI.
                Dpi = new Vector(96, 96);
            }
        }

        public ImmutableBitmap(SKImage image)
        {
            _image = image;
            PixelSize = new PixelSize(image.Width, image.Height);
            Dpi = new Vector(96, 96);
        }

        public ImmutableBitmap(ImmutableBitmap src, PixelSize destinationSize, BitmapInterpolationMode interpolationMode)
        {
            SKImageInfo info = new SKImageInfo(destinationSize.Width, destinationSize.Height, SKColorType.Bgra8888);
            _bitmap = new SKBitmap(info);
            src._image.ScalePixels(_bitmap.PeekPixels(), interpolationMode.ToSKFilterQuality());
            _bitmap.SetImmutable();
            _image = SKImage.FromBitmap(_bitmap);

            PixelSize = new PixelSize(_image.Width, _image.Height);

            // TODO: Skia doesn't have an API for DPI.
            Dpi = new Vector(96, 96);
        }

        public ImmutableBitmap(Stream stream, int decodeSize, bool horizontal, BitmapInterpolationMode interpolationMode)
        {
            using (var skStream = new SKManagedStream(stream))
            using (var skData = SKData.Create(skStream))
            using (var codec = SKCodec.Create(skData))
            {
                var info = codec.Info;

                // get the scale that is nearest to what we want (eg: jpg returned 512)
                var supportedScale = codec.GetScaledDimensions(horizontal ? ((float)decodeSize / info.Width) : ((float)decodeSize / info.Height));

                // decode the bitmap at the nearest size
                var nearest = new SKImageInfo(supportedScale.Width, supportedScale.Height);
                _bitmap = SKBitmap.Decode(codec, nearest);

                if (_bitmap == null)
                    throw new ArgumentException("Unable to load bitmap from provided data");
                
                // now scale that to the size that we want
                var realScale = horizontal ? ((double)info.Height / info.Width) : ((double)info.Width / info.Height);

                SKImageInfo desired;


                if (horizontal)
                {
                    desired = new SKImageInfo(decodeSize, (int)(realScale * decodeSize));
                }
                else
                {
                    desired = new SKImageInfo((int)(realScale * decodeSize), decodeSize);
                }

                if (_bitmap.Width != desired.Width || _bitmap.Height != desired.Height)
                {
                    var scaledBmp = _bitmap.Resize(desired, interpolationMode.ToSKFilterQuality());
                    _bitmap.Dispose();
                    _bitmap = scaledBmp;
                }
                
                _bitmap.SetImmutable();

                _image = SKImage.FromBitmap(_bitmap);

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
        /// <param name="size">Size of the bitmap.</param>
        /// <param name="dpi">DPI of the bitmap.</param>
        /// <param name="stride">Stride of data pixels.</param>
        /// <param name="format">Format of data pixels.</param>
        /// <param name="alphaFormat">Alpha format of data pixels.</param>
        /// <param name="data">Data pixels.</param>
        public ImmutableBitmap(PixelSize size, Vector dpi, int stride, PixelFormat format, AlphaFormat alphaFormat, IntPtr data)
        {
            using (var tmp = new SKBitmap())
            {
                tmp.InstallPixels(
                    new SKImageInfo(size.Width, size.Height, format.ToSkColorType(), alphaFormat.ToSkAlphaType()),
                    data);
                _bitmap = tmp.Copy();
            }
            _bitmap.SetImmutable();
            _image = SKImage.FromBitmap(_bitmap);

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
            _bitmap?.Dispose();
        }

        /// <inheritdoc />
        public void Save(string fileName, int? quality = null)
        {
            ImageSavingHelper.SaveImage(_image, fileName, quality);
        }

        /// <inheritdoc />
        public void Save(Stream stream, int? quality = null)
        {
            ImageSavingHelper.SaveImage(_image, stream, quality);
        }

        /// <inheritdoc />
        public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
        {
            context.Canvas.DrawImage(_image, sourceRect, destRect, paint);
        }

        public PixelFormat? Format => _bitmap?.ColorType.ToAvalonia();

        public AlphaFormat? AlphaFormat => _bitmap?.AlphaType.ToAlphaFormat();

        public ILockedFramebuffer Lock()
        {
            if (_bitmap is null)
                throw new NotSupportedException("A bitmap is needed for locking");

            if (_bitmap.ColorType.ToAvalonia() is not { } format)
                throw new NotSupportedException($"Unsupported format {_bitmap.ColorType}");

            return new LockedFramebuffer(_bitmap.GetPixels(), PixelSize, _bitmap.RowBytes, Dpi, format, null);
        }
    }
}
