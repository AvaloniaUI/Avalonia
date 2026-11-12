using System;
using System.Collections.Generic;
using System.IO;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Skia;
using SkiaSharp;

namespace Avalonia.Impeller;

/// <summary>
/// A layer kept as drawing recordings and replayed into the target, for backends where Impeller can't render
/// to textures. Partial redraws add recordings; once there are too many they are flattened by Skia.
/// </summary>
internal sealed class ImpellerRecordedLayer : IDrawingContextLayerImpl, IImpellerInlineBitmap, IDrawableBitmapImpl,
    IReadableBitmapImpl
{
    private const int MaxRecordings = 8;

    private readonly object _lock = new();
    private readonly List<DrawingRecorder> _recordings = new();
    private ImpellerSkiaRasterLayer? _flattened;
    private SKImage? _snapshot;
    private int _snapshotVersion;

    public ImpellerRecordedLayer(PixelSize size, Vector dpi)
    {
        PixelSize = new PixelSize(Math.Max(1, size.Width), Math.Max(1, size.Height));
        Dpi = dpi;
    }

    public PixelSize PixelSize { get; }
    public Vector Dpi { get; }
    public int Version { get; private set; } = 1;
    public bool CanBlit => true;
    public bool IsCorrupted { get; private set; }

    public PixelFormat? Format => PixelFormats.Rgba8888;
    public AlphaFormat? AlphaFormat => Avalonia.Platform.AlphaFormat.Premul;

    public IDrawingContextImpl CreateDrawingContext()
    {
        ObjectDisposedException.ThrowIf(IsCorrupted, this);
        return new DrawingRecorder(Matrix.Identity, Commit);
    }

    private void Commit(DrawingRecorder recording)
    {
        lock (_lock)
        {
            if (recording.StartsWithClear)
                Reset();

            if (!recording.IsEmpty)
                _recordings.Add(recording);

            if (_recordings.Count > MaxRecordings)
            {
                var flattened = new ImpellerSkiaRasterLayer(PixelSize, new Vector(96, 96));
                using (var context = flattened.CreateDrawingContext())
                    Replay(context);
                Reset();
                _flattened = flattened;
            }

            Version++;
        }
    }

    private void Reset()
    {
        foreach (var old in _recordings)
            old.Release();
        _recordings.Clear();
        _flattened?.Dispose();
        _flattened = null;
    }

    private void Replay(IDrawingContextImpl context)
    {
        if (_flattened is not null)
        {
            var bounds = new Rect(0, 0, PixelSize.Width, PixelSize.Height);
            context.DrawBitmap(_flattened, 1, bounds, bounds);
        }

        foreach (var recording in _recordings)
        {
            context.Transform = Matrix.Identity;
            recording.Replay(context);
        }
    }

    public void Draw(ImpellerDrawingContext context, double opacity, Rect sourceRect, Rect destRect)
    {
        if (sourceRect.Width <= 0 || sourceRect.Height <= 0)
            return;

        var mapping = Matrix.CreateTranslation(-sourceRect.X, -sourceRect.Y)
                      * Matrix.CreateScale(destRect.Width / sourceRect.Width, destRect.Height / sourceRect.Height)
                      * Matrix.CreateTranslation(destRect.X, destRect.Y);

        context.PushClip(destRect);
        // The layer starts transparent, so its clears must not reach the target.
        context.PushOpacity(opacity, destRect);
        lock (_lock)
            context.DrawNested(mapping, Replay);
        context.PopOpacity();
        context.PopClip();
    }

    public void Draw(DrawingContextImpl context, SKRect sourceRect, SKRect destRect, SKSamplingOptions samplingOptions,
        SKPaint paint) =>
        context.Canvas.DrawImage(GetSnapshot(), sourceRect, destRect, samplingOptions, paint);

    private SKImage GetSnapshot()
    {
        lock (_lock)
        {
            if (_snapshot is not null && _snapshotVersion == Version)
                return _snapshot;
            using var raster = new ImpellerSkiaRasterLayer(PixelSize, new Vector(96, 96));
            using (var context = raster.CreateDrawingContext())
                Replay(context);
            _snapshot?.Dispose();
            _snapshot = raster.Snapshot();
            _snapshotVersion = Version;
            return _snapshot;
        }
    }

    public void Blit(IDrawingContextImpl context)
    {
        var dip = new Rect(0, 0, PixelSize.Width / (Dpi.X / 96.0), PixelSize.Height / (Dpi.Y / 96.0));
        context.DrawBitmap(this, 1.0, new Rect(0, 0, PixelSize.Width, PixelSize.Height), dip);
    }

    public void Save(Stream stream, BitmapEncoderOptions options) =>
        Avalonia.Skia.Helpers.ImageSavingHelper.SaveImage(GetSnapshot(), stream, options);

    public ILockedFramebuffer Lock()
    {
        var image = GetSnapshot();
        var bitmap = new SKBitmap(new SKImageInfo(PixelSize.Width, PixelSize.Height, SKColorType.Rgba8888,
            SKAlphaType.Premul));
        image.ReadPixels(bitmap.Info, bitmap.GetPixels(), bitmap.RowBytes);
        return new LockedFramebuffer(bitmap.GetPixels(), PixelSize, bitmap.RowBytes, Dpi, PixelFormats.Rgba8888,
            Avalonia.Platform.AlphaFormat.Premul, bitmap.Dispose);
    }

    public void Dispose()
    {
        lock (_lock)
        {
            IsCorrupted = true;
            Reset();
            _snapshot?.Dispose();
            _snapshot = null;
        }
    }
}
