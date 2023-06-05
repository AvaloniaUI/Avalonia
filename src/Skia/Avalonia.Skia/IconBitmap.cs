using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using Avalonia.Platform;
using SkiaSharp;

namespace Avalonia.Skia;

/// <summary>
/// Represents a Microsoft Windows .ICO file. Contains multiple images at different resolutions;
/// when drawn, the image closest in size to the on-screen render size will be used.
/// </summary>
internal class IconBitmap : IDrawableBitmapImpl
{
    private static readonly Vector s_standardDpi = new(96, 96);

    private readonly SKCodec _codec;
    private readonly Dictionary<SKSize, SKImage> _images;
    private readonly List<IDisposable> _disposables;

    /// <inheritdoc cref="IconBitmap"/>
    public IconBitmap(SKCodec codec)
    {
        _codec = codec ?? throw new ArgumentNullException(nameof(codec));

        if (codec.EncodedFormat != SKEncodedImageFormat.Ico)
        {
            throw new ArgumentException("Codec must contain Ico data.", nameof(codec));
        }

        PixelSize = new(_codec.Info.Width, _codec.Info.Height);

        _images = new(1);
        _disposables = new(2);
    }

    /// <summary>
    /// Gets the size of the largest image within the icon.
    /// </summary>
    public PixelSize PixelSize { get; }

    int IBitmapImpl.Version => 1;

    Vector IBitmapImpl.Dpi => s_standardDpi; // Icons don't really have a DPI

    public void Dispose()
    {
        for (var i = 0; i < _disposables.Count; i++)
        {
            _disposables[i].Dispose();
        }

        _codec.Dispose();
    }

    public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKPaint paint)
    {
        Debug.Assert(_images != null);
        Debug.Assert(_disposables != null);

        var drawTransform = context.Transform.ToSKMatrix(); // relevant when inside a ViewBox
        var dpiScale = Vector.Divide(context.Dpi, s_standardDpi);
        var imageScale = Vector.Divide(ToVector(destRect.Size), ToVector(sourceRect.Size));

        var renderScale = new Size(
            drawTransform.ScaleX * dpiScale.X * imageScale.X,
            drawTransform.ScaleY * dpiScale.Y * imageScale.Y);

        var iconSize = _codec.GetScaledDimensions((float)(renderScale.Width * renderScale.Height)); // yes, we multiply width and height

        if (!_images.TryGetValue(iconSize, out var image))
        {
            var bitmap = SKBitmap.Decode(_codec, new(iconSize.Width, iconSize.Height));
            _disposables.Add(bitmap);
            bitmap.SetImmutable();

            _images[iconSize] = image = SKImage.FromBitmap(bitmap);
            _disposables.Add(image);
        }

        var sourceRectFactor = image.Width / (float)_codec.Info.Width;
        context.Canvas.DrawImage(image, ScaleRect(sourceRect, sourceRectFactor), destRect, paint);
    }

    private static SKRect ScaleRect(SKRect rect, float factor) => new(rect.Left * factor, rect.Top * factor, rect.Right * factor, rect.Bottom * factor);
    private static Vector ToVector(SKSize size) => new(size.Width, size.Height);

    void IBitmapImpl.Save(string fileName, int? quality) => throw new NotSupportedException();
    void IBitmapImpl.Save(Stream stream, int? quality) => throw new NotSupportedException();
}

